using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Exceptions;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using BLL.DTOs;
using BLL.DTOs.Business;
using BLL.DTOs.Fleet;
using BLL.Services.Interface;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class BusinessBookingService : IBusinessBookingService
    {
        private readonly AutoWashDbContext _context;
        private readonly ILaneSchedulerService _laneSchedulerService;

        public BusinessBookingService(AutoWashDbContext context, ILaneSchedulerService laneSchedulerService)
        {
            _context = context;
            _laneSchedulerService = laneSchedulerService;
        }

        public async Task<List<DTOs.Business.TimeSlotResponseDTO>> GetAvailableSlotsForBusinessAsync(int businessUserId, CheckBusinessSlotsRequestDTO request)
        {
            var business = await _context.BusinessProfiles
                .FirstOrDefaultAsync(x =>
                    x.UserId == businessUserId &&
                    x.ApprovalStatus == "Approved");

            if (business == null)
                throw new NotFoundException("Không tìm thấy hồ sơ doanh nghiệp hoặc chưa được phê duyệt.");

            var representativeVehicle = await _context.FleetVehicles
                .Include(x => x.VehicleType)
                .FirstOrDefaultAsync(x =>
                    x.FleetVehicleId == request.FleetVehicleId &&
                    x.BusinessProfileId == business.BusinessProfileId &&
                    x.Status == "Active");

            if (representativeVehicle == null)
                throw new NotFoundException("Không tìm thấy phương tiện hoặc phương tiện chưa được kích hoạt.");

            // ── Timezone ─────────────────────────────────────────────────────
            TimeZoneInfo vnTimeZone;
            try { vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
            catch { vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }

            DateTime todayInVN = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone).Date;
            TimeSpan currentTimeInVN = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone).TimeOfDay;

            if (request.TargetDate.Date < todayInVN)
                throw new BadRequestException("Không thể đặt lịch cho ngày trong quá khứ.");

            // ── Build schedule requests for N vehicles ────────────────────────
            // All vehicles in a fleet booking share the same type and services,
            // so we repeat the representative vehicle N times for the simulation.
            var servicePrices = await _context.ServicePrices
                .Where(x =>
                    x.BranchId == request.BranchId &&
                    x.VehicleTypeId == representativeVehicle.VehicleTypeId &&
                    request.ServiceIds.Contains(x.ServiceId))
                .ToListAsync();

            // If no services selected yet fall back to base weight only (conservative estimate)
            if (!servicePrices.Any() && request.ServiceIds.Any())
                throw new BadRequestException("Một hoặc nhiều dịch vụ không tồn tại hoặc chưa được cấu hình giá.");

            var simRequests = Enumerable.Range(0, (int)request.VehicleCount)
                .Select(_ => new VehicleScheduleRequest
                {
                    FleetVehicleId = 0, // simulation — no real ID needed
                    VehicleType = representativeVehicle.VehicleType,
                    ServicePrices = servicePrices
                })
                .ToList();

            // ── Check each slot ───────────────────────────────────────────────
            var allSlots = await _context.TimeSlots
                .Where(s => s.BranchId == request.BranchId)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            var response = new List<DTOs.Business.TimeSlotResponseDTO>();

            foreach (var slot in allSlots)
            {
                var slotDto = new DTOs.Business.TimeSlotResponseDTO
                {
                    SlotId = slot.SlotId,
                    TimeRange = $"{slot.StartTime:hh\\:mm} - {slot.EndTime:hh\\:mm}",
                    IsAvailable = true,
                    Reason = "Trống"
                };

                // Past-time guard
                if (request.TargetDate.Date == todayInVN && slot.StartTime < currentTimeInVN)
                {
                    slotDto.IsAvailable = false;
                    slotDto.Reason = "Đã qua giờ";
                    response.Add(slotDto);
                    continue;
                }

                DateTime slotStart = request.TargetDate.Date.Add(slot.StartTime);
                TimeSpan slotDuration = slot.EndTime - slot.StartTime;

                var simResult = await _laneSchedulerService.ScheduleFleetAsync(
                    request.BranchId, slotStart, slotDuration, simRequests);

                if (!simResult.Success)
                {
                    slotDto.IsAvailable = false;
                    slotDto.Reason = simResult.ErrorMessage!;
                }
                else
                {
                    // Show how deep into the slot the last vehicle finishes
                    var lastEnd = simResult.Assignments.Max(a => a.EstimatedEnd);
                    slotDto.EstimatedLastEndMinutesIntoSlot =
                        (int)(lastEnd - slotStart).TotalMinutes;
                }

                response.Add(slotDto);
            }

            return response;
        }

        public async Task<MultiVehicleBookingResponseDTO> CreateBookingAsync(int businessUserId, CreateBusinessBookingDTO dto)
        {
            // ── Validate business ─────────────────────────────────────────────
            var business = await _context.BusinessProfiles
                .FirstOrDefaultAsync(x =>
                    x.UserId == businessUserId &&
                    x.ApprovalStatus == "Approved");

            if (business == null)
                throw new NotFoundException("Không tìm thấy hồ sơ doanh nghiệp.");

            // ── Validate all fleet vehicles belong to this business ────────────
            var fleetVehicles = await _context.FleetVehicles
                .Include(x => x.VehicleType)
                .Where(x =>
                    dto.FleetVehicleIds.Contains(x.FleetVehicleId) &&
                    x.BusinessProfileId == business.BusinessProfileId)
                .ToListAsync();

            if (fleetVehicles.Count != dto.FleetVehicleIds.Count)
                throw new NotFoundException("Một hoặc nhiều phương tiện không thuộc về doanh nghiệp này.");

            var inactiveVehicle = fleetVehicles.FirstOrDefault(x => x.Status != "Active");
            if (inactiveVehicle != null)
                throw new BadRequestException(
                    $"Phương tiện {inactiveVehicle.LicensePlate} chưa được kích hoạt.");

            // ── Validate branch + slot ────────────────────────────────────────
            var branch = await _context.Branches
                .FirstOrDefaultAsync(x => x.BranchId == dto.BranchId);

            if (branch == null)
                throw new NotFoundException("Không tìm thấy chi nhánh.");

            var slot = await _context.TimeSlots
                .FirstOrDefaultAsync(x =>
                    x.SlotId == dto.SlotId &&
                    x.BranchId == dto.BranchId);

            if (slot == null)
                throw new NotFoundException("Không tìm thấy khung giờ.");

            DateTime scheduledTime = dto.ScheduledTime.Date.Add(slot.StartTime);
            TimeSpan slotDuration = slot.EndTime - slot.StartTime;

            // ── Validate services + compute total price ───────────────────────
            var services = await _context.Services
                .Where(x => dto.ServiceIds.Contains(x.ServiceId))
                .ToListAsync();

            if (services.Count != dto.ServiceIds.Count)
                throw new BadRequestException("Một hoặc nhiều dịch vụ không tồn tại.");

            // Pre-load all service prices for all vehicle types in this booking
            var allVehicleTypeIds = fleetVehicles
                .Select(x => x.VehicleTypeId)
                .Distinct()
                .ToList();

            var allServicePrices = await _context.ServicePrices
                .Where(x =>
                    x.BranchId == dto.BranchId &&
                    allVehicleTypeIds.Contains(x.VehicleTypeId) &&
                    dto.ServiceIds.Contains(x.ServiceId))
                .ToListAsync();

            // ── Run EAL simulation ────────────────────────────────────────────
            var scheduleRequests = fleetVehicles
                .Select(v => new VehicleScheduleRequest
                {
                    FleetVehicleId = v.FleetVehicleId,
                    VehicleType = v.VehicleType,
                    ServicePrices = allServicePrices
                        .Where(sp => sp.VehicleTypeId == v.VehicleTypeId)
                        .ToList()
                })
                .ToList();

            var scheduleResult = await _laneSchedulerService.ScheduleFleetAsync(
                dto.BranchId, scheduledTime, slotDuration, scheduleRequests);

            if (!scheduleResult.Success)
                throw new BadRequestException(scheduleResult.ErrorMessage!);

            // ── Validate lane names for response ──────────────────────────────
            var laneIds = scheduleResult.Assignments.Select(a => a.LaneId).Distinct().ToList();
            var laneNames = await _context.Lanes
                .Where(x => laneIds.Contains(x.LaneId))
                .ToDictionaryAsync(x => x.LaneId, x => x.Name);

            // ── Persist one Booking + BookingDetails per vehicle ──────────────
            var vehicleSummaries = new List<VehicleBookingSummaryDTO>();
            decimal totalAmount = 0;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var vehicle in fleetVehicles)
                {
                    var assignment = scheduleResult.Assignments
                        .First(a => a.FleetVehicleId == vehicle.FleetVehicleId);

                    var vehicleServicePrices = allServicePrices
                        .Where(sp => sp.VehicleTypeId == vehicle.VehicleTypeId)
                        .ToList();

                    // Validate every service has a price for this vehicle type
                    foreach (var service in services)
                    {
                        bool hasPrice = vehicleServicePrices
                            .Any(sp => sp.ServiceId == service.ServiceId);

                        if (!hasPrice)
                            throw new BadRequestException(
                                $"Chưa cấu hình giá cho dịch vụ '{service.ServiceName}' " +
                                $"với loại xe '{vehicle.VehicleType.Name}'.");
                    }

                    decimal vehicleTotal = vehicleServicePrices
                        .Where(sp => dto.ServiceIds.Contains(sp.ServiceId))
                        .Sum(sp => sp.Price);

                    // Update slot capacity weight
                    var dailyCapacity = await _context.DailySlotCapacities
                        .FirstOrDefaultAsync(x =>
                            x.BranchId == dto.BranchId &&
                            x.SlotId == dto.SlotId &&
                            x.Date == dto.ScheduledTime.Date);

                    if (dailyCapacity == null)
                    {
                        dailyCapacity = new DailySlotCapacity
                        {
                            BranchId = dto.BranchId,
                            SlotId = dto.SlotId,
                            Date = dto.ScheduledTime.Date,
                            BookedWeight = 0
                        };
                        _context.DailySlotCapacities.Add(dailyCapacity);
                    }

                    if (dailyCapacity.BookedWeight + vehicle.VehicleType.BaseWeight > slot.MaxCapacity)
                        throw new BadRequestException(
                            $"Khung giờ đã hết sức chứa cho phương tiện {vehicle.LicensePlate}.");

                    dailyCapacity.BookedWeight += vehicle.VehicleType.BaseWeight;

                    // Create booking
                    var booking = new Booking
                    {
                        BusinessProfileId = business.BusinessProfileId,
                        FleetVehicleId = vehicle.FleetVehicleId,
                        BookingType = "Business",
                        BranchId = dto.BranchId,
                        ScheduledTime = scheduledTime,
                        LicensePlate = vehicle.LicensePlate,
                        Status = "Pending",
                        OriginalPrice = vehicleTotal,
                        FinalAmount = vehicleTotal,
                        CapacityWeight = vehicle.VehicleType.BaseWeight,
                        FallbackQrCode = Guid.NewGuid().ToString("N")[..8].ToUpper(),
                        ProcessingLaneId = assignment.LaneId  // lane reserved at booking time
                    };

                    _context.Bookings.Add(booking);

                    foreach (var service in services)
                    {
                        var sp = vehicleServicePrices.First(x => x.ServiceId == service.ServiceId);
                        _context.BookingDetails.Add(new BookingDetail
                        {
                            Booking = booking,
                            ServiceId = service.ServiceId,
                            Price = sp.Price
                        });
                    }

                    totalAmount += vehicleTotal;

                    vehicleSummaries.Add(new VehicleBookingSummaryDTO
                    {
                        // BookingId filled after SaveChanges below
                        LicensePlate = vehicle.LicensePlate,
                        LaneId = assignment.LaneId,
                        LaneName = laneNames.TryGetValue(assignment.LaneId, out var ln) ? ln : "",
                        EstimatedStart = assignment.EstimatedStart,
                        EstimatedEnd = assignment.EstimatedEnd,
                        Amount = vehicleTotal
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            // Back-fill BookingIds now that EF has assigned them
            var savedBookings = await _context.Bookings
                .Where(x =>
                    x.BusinessProfileId == business.BusinessProfileId &&
                    x.ScheduledTime == scheduledTime &&
                    x.Status == "Pending")
                .OrderBy(x => x.BookingId)
                .Select(x => new { x.BookingId, x.LicensePlate })
                .ToListAsync();

            foreach (var summary in vehicleSummaries)
            {
                var match = savedBookings.FirstOrDefault(b => b.LicensePlate == summary.LicensePlate);
                if (match != null)
                    summary.BookingId = match.BookingId;
            }

            return new MultiVehicleBookingResponseDTO
            {
                BookingGroupId = vehicleSummaries.First().BookingId,
                TotalVehicles = vehicleSummaries.Count,
                TotalAmount = totalAmount,
                Status = "Pending",
                Vehicles = vehicleSummaries
            };
        }

        //public async Task<BusinessBookingResponseDTO> CreateBookingAsync(int businessUserId, CreateBusinessBookingDTO dto)
        //{
        //    var business = await _context.BusinessProfiles
        //        .FirstOrDefaultAsync(x =>
        //            x.UserId == businessUserId &&
        //            x.ApprovalStatus == "Approved");

        //    if (business == null)
        //    {
        //        throw new NotFoundException("Không tìm thấy hồ sơ doanh nghiệp.");
        //    }

        //    var fleetVehicle = await _context.FleetVehicles.Include(x => x.VehicleType)
        //        .FirstOrDefaultAsync(x =>
        //            x.FleetVehicleId == dto.FleetVehicleId &&
        //            x.BusinessProfileId == business.BusinessProfileId);

        //    if (fleetVehicle == null)
        //    {
        //        throw new NotFoundException("Không tìm thấy phương tiện trong đội xe.");
        //    }

        //    if (fleetVehicle.Status != "Active")
        //    {
        //        throw new BadRequestException( "Phương tiện chưa được kích hoạt.");
        //    }

        //    var branch = await _context.Branches
        //        .FirstOrDefaultAsync(x => x.BranchId == dto.BranchId);

        //    if (branch == null)
        //    {
        //        throw new NotFoundException("Không tìm thấy chi nhánh.");
        //    }

        //    var slot = await _context.TimeSlots
        //        .FirstOrDefaultAsync(x => x.SlotId == dto.SlotId && x.BranchId == dto.BranchId);

        //    if (slot == null)
        //    {
        //        throw new NotFoundException("Không tìm thấy khung giờ.");
        //    }

        //    var scheduledTime = dto.ScheduledTime.Date.Add(slot.StartTime);

        //    var dailyCapacity = await _context.DailySlotCapacities
        //        .FirstOrDefaultAsync(x => x.BranchId == dto.BranchId && x.SlotId == dto.SlotId && x.Date == dto.ScheduledTime.Date);

        //    if (dailyCapacity == null)
        //    {
        //        dailyCapacity = new DailySlotCapacity
        //        {
        //            BranchId = dto.BranchId,
        //            SlotId = dto.SlotId,
        //            Date = dto.ScheduledTime.Date,
        //            BookedWeight = 0
        //        };
        //        _context.DailySlotCapacities.Add(dailyCapacity);
        //    }

        //    if (dailyCapacity.BookedWeight + fleetVehicle.VehicleType.BaseWeight > slot.MaxCapacity)
        //    {
        //        throw new BadRequestException("Khung giờ này đã hết chỗ.");
        //    }

        //    dailyCapacity.BookedWeight += fleetVehicle.VehicleType.BaseWeight;

        //    var services = await _context.Services
        //        .Where(x => dto.ServiceIds.Contains(x.ServiceId))
        //        .ToListAsync();

        //    if (services.Count != dto.ServiceIds.Count)
        //    {
        //        throw new BadRequestException("Một hoặc nhiều dịch vụ không tồn tại.");
        //    }

        //    decimal totalPrice = 0;

        //    foreach (var service in services)
        //    {
        //        var servicePrice = await _context.ServicePrices
        //            .FirstOrDefaultAsync(x =>
        //                x.ServiceId == service.ServiceId &&
        //                x.VehicleTypeId == fleetVehicle.VehicleTypeId &&
        //                x.BranchId == dto.BranchId);

        //        if (servicePrice == null)
        //        {
        //            throw new BadRequestException($"Chưa cấu hình giá cho dịch vụ {service.ServiceName}.");
        //        }

        //        totalPrice += servicePrice.Price;
        //    }

        //    var booking = new Booking
        //    {
        //        BusinessProfileId = business.BusinessProfileId,
        //        FleetVehicleId = fleetVehicle.FleetVehicleId,
        //        BookingType = "Business",
        //        BranchId = dto.BranchId,
        //        ScheduledTime = scheduledTime,
        //        LicensePlate = fleetVehicle.LicensePlate,
        //        Status = "Pending",
        //        OriginalPrice = totalPrice,
        //        FinalAmount = totalPrice,
        //        CapacityWeight = fleetVehicle.VehicleType.BaseWeight,
        //        FallbackQrCode = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()
        //    };

        //    _context.Bookings.Add(booking);

        //    foreach (var service in services)
        //    {
        //        var servicePrice = await _context.ServicePrices
        //            .FirstAsync(x =>
        //                x.ServiceId == service.ServiceId &&
        //                x.VehicleTypeId == fleetVehicle.VehicleTypeId &&
        //                x.BranchId == dto.BranchId);

        //        _context.BookingDetails.Add(new BookingDetail
        //        {
        //            Booking = booking,
        //            ServiceId = service.ServiceId,
        //            Price = servicePrice.Price
        //        });
        //    }

        //    await _context.SaveChangesAsync();

        //    return new BusinessBookingResponseDTO
        //    {
        //        BookingId = booking.BookingId,
        //        LicensePlate = booking.LicensePlate,
        //        OriginalPrice = booking.OriginalPrice,
        //        FinalAmount = booking.FinalAmount,
        //        Status = booking.Status
        //    };
        //}

        public async Task<List<FleetVehicleDTO>> GetActiveFleetVehiclesAsync(int businessUserId)
        {
            var business = await _context.BusinessProfiles
                .FirstOrDefaultAsync(x => x.UserId == businessUserId);

            if (business == null) throw new NotFoundException("Không tìm thấy hồ sơ doanh nghiệp.");

            return await _context.FleetVehicles
                .Include(x => x.VehicleType)
                .Where(x =>
                    x.BusinessProfileId == business.BusinessProfileId &&
                    x.Status == "Active")
                .Select(x => new FleetVehicleDTO
                {
                    FleetVehicleId = x.FleetVehicleId,
                    LicensePlate = x.LicensePlate,
                    Brand = x.Brand,
                    Model = x.Model,
                    VehicleTypeName = x.VehicleType.Name,
                    DriverName = x.DriverName,
                    EmployeeId = x.EmployeeCode,
                    Status = x.Status
                })
                .ToListAsync();
        }

        public async Task<List<BusinessBookingListDTO>> GetBookingsAsync(int businessUserId)
        {
            var business = await _context.BusinessProfiles
                .FirstOrDefaultAsync(x => x.UserId == businessUserId);

            if (business == null) throw new NotFoundException("Không tìm thấy hồ sơ doanh nghiệp.");

            return await _context.Bookings
                .Where(x =>
                    x.BusinessProfileId ==
                    business.BusinessProfileId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new BusinessBookingListDTO
                {
                    BookingId = x.BookingId,
                    LicensePlate = x.LicensePlate,
                    ScheduledTime = x.ScheduledTime,
                    Status = x.Status,
                    FinalAmount = x.FinalAmount
                })
                .ToListAsync();
        }

        public async Task<BusinessBookingDetailDTO> GetBookingDetailAsync(int businessUserId, int bookingId)
        {
            var business = await _context.BusinessProfiles
                .FirstOrDefaultAsync(x => x.UserId == businessUserId);

            if (business == null)
            {
                throw new NotFoundException("Không tìm thấy hồ sơ doanh nghiệp.");
            }

            var booking = await _context.Bookings
                .Include(x => x.BookingDetails)
                    .ThenInclude(x => x.Service)
                .FirstOrDefaultAsync(x =>
                    x.BookingId == bookingId &&
                    x.BusinessProfileId == business.BusinessProfileId);

            if (booking == null)
            {
                throw new NotFoundException("Không tìm thấy lịch đặt.");
            }

            return new BusinessBookingDetailDTO
            {
                BookingId = booking.BookingId,
                LicensePlate = booking.LicensePlate,
                ScheduledTime = booking.ScheduledTime,
                Status = booking.Status,
                OriginalPrice = booking.OriginalPrice,
                FinalAmount = booking.FinalAmount,
                Services = booking.BookingDetails
                    .Select(x => x.Service.ServiceName)
                    .ToList()
            };
        }

        public async Task CancelBookingAsync(int businessUserId, int bookingId)
        {
            var business = await _context.BusinessProfiles
                .FirstOrDefaultAsync(x => x.UserId == businessUserId);

            if (business == null)
            {
                throw new NotFoundException("Không tìm thấy hồ sơ doanh nghiệp.");
            }

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(x =>
                    x.BookingId == bookingId &&
                    x.BusinessProfileId == business.BusinessProfileId);

            if (booking == null)
            {
                throw new NotFoundException("Không tìm thấy lịch đặt.");
            }

            if (booking.Status != "Pending")
            {
                throw new BadRequestException("Chỉ có thể hủy lịch đặt đang ở trạng thái chờ.");
            }

            var slot = await _context.TimeSlots
                .FirstOrDefaultAsync(x =>
                    x.BranchId == booking.BranchId &&
                    x.StartTime == booking.ScheduledTime.TimeOfDay);

            if (slot != null)
            {
                var dailyCapacity = await _context.DailySlotCapacities
                    .FirstOrDefaultAsync(x =>
                        x.BranchId == booking.BranchId &&
                        x.SlotId == slot.SlotId &&
                        x.Date == booking.ScheduledTime.Date);

                if (dailyCapacity != null)
                {
                    dailyCapacity.BookedWeight -= booking.CapacityWeight;

                    if (dailyCapacity.BookedWeight < 0)
                    {
                        dailyCapacity.BookedWeight = 0;
                    }
                }
            }

            booking.Status = "Cancelled";
            booking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<FleetWashLogDTO> CheckInAsync(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(x => x.FleetVehicle)
                .Include(x => x.BookingDetails)
                .FirstOrDefaultAsync(x =>
                    x.BookingId == bookingId);

            if (booking == null) throw new NotFoundException("Không tìm thấy lịch đặt.");

            if (booking.BookingType != "Business") throw new BadRequestException("Không phải lịch đặt của doanh nghiệp.");

            if (booking.Status != "Pending") throw new BadRequestException("Lịch đặt này chưa thể Check-in.");

            var detail = booking.BookingDetails.First();

            var washLog = new FleetWashLog
            {
                FleetVehicleId = booking.FleetVehicleId!.Value,
                BranchId = booking.BranchId,
                BookingId = booking.BookingId,
                CheckInTime = DateTime.UtcNow,
                WashCost = booking.FinalAmount,
                Status = "CheckedIn"
            };

            _context.FleetWashLogs.Add(washLog);

            booking.Status = "CheckedIn";

            await _context.SaveChangesAsync();

            return new FleetWashLogDTO
            {
                FleetWashLogId = washLog.FleetWashLogId,
                LicensePlate = booking.LicensePlate,
                CheckInTime = washLog.CheckInTime,
                Status = washLog.Status
            };
        }

        public async Task<FleetCheckInResponseDTO> WalkInAsync(FleetWalkInDTO dto)
        {
            var vehicle = await _context.FleetVehicles
                .FirstOrDefaultAsync(x =>
                    x.LicensePlate == dto.LicensePLate &&
                    x.Status == "Active");

            if (vehicle == null)
            {
                throw new NotFoundException("Không tìm thấy phương tiện trong đội xe.");
            }

            var branch = await _context.Branches
                .FirstOrDefaultAsync(x =>
                    x.BranchId == dto.BranchId);

            if (branch == null)
            {
                throw new NotFoundException("Không tìm thấy chi nhánh.");
            }

            var existingLog = await _context.FleetWashLogs
                .FirstOrDefaultAsync(x =>
                    x.FleetVehicleId == vehicle.FleetVehicleId &&
                    (x.Status == "CheckedIn" ||
                     x.Status == "Processing"));

            if (existingLog != null)
            {
                throw new BadRequestException("Phương tiện này đang trong quá trình rửa xe.");
            }

            var washLog = new FleetWashLog
            {
                FleetVehicleId = vehicle.FleetVehicleId,
                BranchId = dto.BranchId,
                BookingId = null,
                CheckInTime = DateTime.UtcNow,
                Status = "CheckedIn",
                WashCost = 0
            };

            _context.FleetWashLogs.Add(washLog);

            await _context.SaveChangesAsync();

            return new FleetCheckInResponseDTO
            {
                FleetWashLogId = washLog.FleetWashLogId,
                FleetVehicleId = vehicle.FleetVehicleId,
                LicensePlate = vehicle.LicensePlate,
                DriverName = vehicle.DriverName,
                CheckInTime = washLog.CheckInTime,
                Status = washLog.Status!
            };
        }

        public async Task WalkOutAsync(int washLogId)
        {
            var washLog = await _context.FleetWashLogs
                .FirstOrDefaultAsync(x => x.FleetWashLogId == washLogId);

            if (washLog == null)
            {
                throw new NotFoundException("Không tìm thấy nhật ký rửa xe.");
            }

            if (washLog.Status != "Processing")
            {
                throw new BadRequestException("Phương tiện phải đang ở trạng thái xử lý.");
            }

            washLog.Status = "Completed";
            washLog.CompletedTime = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task StartProcessingAsync(int washLogId, int staffUserId, StartFleetWashDTO dto)
        {
            var washLog = await _context.FleetWashLogs
                .Include(x => x.Booking)
                .FirstOrDefaultAsync(x =>
                    x.FleetWashLogId == washLogId);

            if (washLog == null)
            {
                throw new NotFoundException("Không tìm thấy nhật ký rửa xe.");
            }

            if (washLog.Status != "Assigned")
            {
                throw new BadRequestException("Phương tiện không ở trạng thái chờ xử lý.");
            }

            var lane = await _context.Lanes
                .FirstOrDefaultAsync(x => x.LaneId == dto.LaneId);

            if (lane == null)
            {
                throw new NotFoundException("Không tìm thấy làn rửa.");
            }

            washLog.Status = "Processing";

            if (washLog.Booking != null)
            {
                washLog.Booking.ProcessingLaneId = dto.LaneId;
                washLog.Booking.ProcessingStaffId = staffUserId;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<CurrentFleetVehicleDTO>> GetCurrentVehiclesAsync()
        {
            return await _context.FleetWashLogs
                .Include(x => x.FleetVehicle)
                .Where(x =>
                    x.Status == "CheckedIn" ||
                    x.Status == "Processing")
                .OrderBy(x => x.CheckInTime)
                .Select(x => new CurrentFleetVehicleDTO
                {
                    FleetWashLogId = x.FleetWashLogId,
                    LicensePlate = x.FleetVehicle.LicensePlate,
                    DriverName = x.FleetVehicle.DriverName,
                    Status = x.Status!,
                    CheckInTime = x.CheckInTime
                })
                .ToListAsync();
        }

        public async Task<FleetCheckoutResponseDTO> CheckOutAsync(int washLogId)
        {
            var washLog = await _context.FleetWashLogs
                .Include(x => x.Booking)
                    .ThenInclude(x => x.BookingDetails)
                .FirstOrDefaultAsync(x =>
                    x.FleetWashLogId == washLogId &&
                    x.Status != "Completed");

            if (washLog == null)
            {
                throw new NotFoundException("Không tìm thấy nhật ký rửa xe.");
            }

            if (washLog.BookingId.HasValue)
            {
                var booking = washLog.Booking!;

            }

            if (washLog.Status != "Processing")
            {
                throw new BadRequestException("Chỉ có thể checkout phương tiện đang ở trạng thái xử lý.");
            }

            washLog.Status = "Completed";
            washLog.CompletedTime = DateTime.UtcNow;

            if (washLog.Booking.Status != null)
            {
                washLog.Booking.Status = "Completed";
            }

            await _context.SaveChangesAsync();

            return new FleetCheckoutResponseDTO
            {
                FleetWashLogId = washLog.FleetWashLogId,
                CompletedTime = washLog.CompletedTime.Value
            };
        }

        public async Task<InvoiceDTO> GetInvoiceByBookingAsync(int bookingId)
        {
            var invoice = await _context.Invoices
                .Include(x => x.InvoiceItems)
                .FirstOrDefaultAsync(x => x.BookingId == bookingId);

            if (invoice == null)
                throw new NotFoundException("Không tìm thấy hóa đơn.");

            return new InvoiceDTO
            {
                InvoiceId = invoice.InvoiceId,
                InvoiceCode = invoice.InvoiceCode,
                Subtotal = invoice.Subtotal,
                TaxAmount = invoice.TaxAmount,
                TotalAmount = invoice.TotalAmount,
                Status = invoice.Status,
                IssuedAt = invoice.IssuedAt,
                Items = invoice.InvoiceItems.Select(x => new InvoiceItemDTO
                {
                    Description = x.Description,
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice,
                    Amount = x.Amount
                }).ToList()
            };
        }

        public async Task<List<FleetWashHistoryDTO>> GetFleetWashHistoryAsync(int businessUserId, FleetHistoryFilterDTO filter)
        {
            var business = await _context.BusinessProfiles
                .FirstOrDefaultAsync(x => x.UserId == businessUserId);

            if (business == null) throw new NotFoundException("Không tìm thấy hồ sơ doanh nghiệp.");

            var query = _context.FleetWashLogs
                .Include(x => x.FleetVehicle)
                    .ThenInclude(x => x.VehicleType)
                .Include(x => x.Booking)
                    .ThenInclude(x => x.Branch)
                .Where(x => x.FleetVehicle.BusinessProfileId == business.BusinessProfileId)
                .AsQueryable();

            if (filter.FleetVehicleId.HasValue)
            {
                query = query.Where(x => x.FleetVehicleId == filter.FleetVehicleId.Value);
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(x => x.CheckInTime >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(x => x.CheckInTime <= filter.ToDate.Value);
            }

            return await query
                .OrderByDescending(x => x.CheckInTime)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(x => new FleetWashHistoryDTO
                {
                    FleetWashLogId = x.FleetWashLogId,
                    LicensePlate = x.FleetVehicle.LicensePlate,
                    VehicleType = x.FleetVehicle.VehicleType.Name,
                    BranchName =
                        x.Booking != null
                            ? x.Booking.Branch.Name
                            : "Walk-In",
                    CheckInTime = x.CheckInTime,
                    CompletedTime = x.CompletedTime,
                    Status = x.Status!,
                    WashCost = x.WashCost,
                    BookingId = x.BookingId,
                    WashType =
                        x.BookingId != null
                            ? "Booking"
                            : "WalkIn"
                })
                .ToListAsync();
        }

        public async Task<FleetDashboardDTO> GetDashboardAsync(int businessUserId)
        {
            var business = await _context.BusinessProfiles
                .FirstOrDefaultAsync(x => x.UserId == businessUserId);

            if (business == null) throw new NotFoundException("Không tìm thấy hồ sơ doanh nghiệp.");

            var today = DateTime.Today;

            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

            var vehicleIds = await _context.FleetVehicles
                .Where(x => x.BusinessProfileId == business.BusinessProfileId)
                .Select(x => x.FleetVehicleId)
                .ToListAsync();

            return new FleetDashboardDTO
            {
                TotalVehicles = await _context.FleetVehicles
                    .CountAsync(x => x.BusinessProfileId == business.BusinessProfileId),

                ActiveVehicles = await _context.FleetVehicles
                    .CountAsync(x => x.BusinessProfileId == business.BusinessProfileId && x.Status == "Active"),

                PendingVehicles = await _context.FleetVehicles
                    .CountAsync(x => x.BusinessProfileId == business.BusinessProfileId && x.Status == "PendingApproval"),

                TodayWashCount = await _context.FleetWashLogs
                    .CountAsync(x => vehicleIds.Contains(x.FleetVehicleId) && x.CheckInTime.Date == today),

                MonthlyWashCount = await _context.FleetWashLogs
                    .CountAsync(x => vehicleIds.Contains(x.FleetVehicleId) && x.CheckInTime >= firstDayOfMonth),

                MonthlySpend = await _context.FleetWashLogs
                        .Where(x => vehicleIds.Contains(x.FleetVehicleId) && x.CheckInTime >= firstDayOfMonth)
                        .SumAsync(x => (decimal?)x.WashCost) ?? 0,

                VehiclesCurrentlyInStation = await _context.FleetWashLogs
                    .CountAsync(x => vehicleIds.Contains(x.FleetVehicleId) && x.Status != "Completed" && x.Status != "Cancelled")
            };
        }

        public async Task<List<InvoiceListDTO>> GetInvoicesAsync(int businessUserId)
        {
            var business = await _context.BusinessProfiles
                .FirstOrDefaultAsync(x => x.UserId == businessUserId);

            if (business == null) throw new NotFoundException("Không tìm thấy hồ sơ doanh nghiệp.");

            return await _context.Invoices
                .Include(x => x.Booking)
                .Where(x => x.BusinessProfileId == business.BusinessProfileId)
                .OrderByDescending(x => x.IssuedAt)
                .Select(x => new InvoiceListDTO
                {
                    InvoiceId = x.InvoiceId,
                    InvoiceCode = x.InvoiceCode,
                    IssuedAt = x.IssuedAt,
                    TotalAmount = x.TotalAmount,
                    Status = x.Status,
                    LicensePlate = x.Booking.LicensePlate
                })
                .ToListAsync();
        }

        public async Task<InvoiceDetailDTO> GetInvoiceDetailAsync(int businessUserId, int invoiceId)
        {
            var business = await _context.BusinessProfiles
                .FirstOrDefaultAsync(x => x.UserId == businessUserId);

            if (business == null) throw new NotFoundException("Không tìm thấy hồ sơ doanh nghiệp.");

            var invoice = await _context.Invoices
                .Include(x => x.Booking)
                .Include(x => x.InvoiceItems)
                .FirstOrDefaultAsync(x => x.InvoiceId == invoiceId && x.BusinessProfileId == business.BusinessProfileId);

            if (invoice == null) throw new NotFoundException("Không tìm thấy hóa đơn.");

            return new InvoiceDetailDTO
            {
                InvoiceId = invoice.InvoiceId,
                InvoiceCode = invoice.InvoiceCode,
                IssuedAt = invoice.IssuedAt,
                Subtotal = invoice.Subtotal,
                TaxAmount = invoice.TaxAmount,
                TotalAmount = invoice.TotalAmount,
                Status = invoice.Status,
                LicensePlate = invoice.Booking.LicensePlate,

                Items = invoice.InvoiceItems
                    .Select(i => new InvoiceItemDTO
                    {
                        InvoiceItemId = i.InvoiceItemId,
                        Description = i.Description,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        Amount = i.Amount
                    })
                    .ToList()
            };
        }

        public async Task<MonthlyStatementDTO> GetMonthlyStatementAsync(int businessUserId, int year, int month)
        {
            var business = await _context.BusinessProfiles.FirstOrDefaultAsync(x => x.UserId == businessUserId);

            if (business == null) throw new NotFoundException("Không tìm thấy hồ sơ doanh nghiệp.");

            var startDate = new DateTime(year, month, 1);

            var endDate = startDate.AddMonths(1);

            var logs = await _context.FleetWashLogs
                .Include(x => x.FleetVehicle)
                .Where(x =>
                    x.FleetVehicle.BusinessProfileId ==
                    business.BusinessProfileId &&
                    x.CheckInTime >= startDate &&
                    x.CheckInTime < endDate)
                .ToListAsync();

            return new MonthlyStatementDTO
            {
                Year = year,
                Month = month,
                TotalWashes = logs.Count,
                TotalCost = logs.Sum(x => x.WashCost),
                Vehicles = logs
                    .GroupBy(x => new
                    {
                        x.FleetVehicleId,
                        x.FleetVehicle.LicensePlate
                    })
                    .Select(g => new VehicleStatementDTO
                    {
                        FleetVehicleId = g.Key.FleetVehicleId,
                        LicensePlate = g.Key.LicensePlate,
                        WashCount = g.Count(),
                        TotalCost = g.Sum(x => x.WashCost)
                    })
                    .OrderByDescending(x => x.TotalCost)
                    .ToList()
            };
        }

        public async Task AssignLaneAsync(int washLogId, AssignLaneDTO dto)
        {
            var washLog = await _context.FleetWashLogs.FirstOrDefaultAsync(x => x.FleetWashLogId == washLogId);

            if (washLog == null)
            {
                throw new NotFoundException("Không tìm thấy nhật ký rửa xe.");
            }

            if (washLog.Status != "CheckedIn")
            {
                throw new BadRequestException("Phương tiện không ở trạng thái chờ phân công.");
            }

            var lane = await _context.Lanes.FirstOrDefaultAsync(x => x.LaneId == dto.LaneId);

            if (lane == null)
            {
                throw new NotFoundException("Không tìm thấy làn rửa.");
            }

            var staff = await _context.Users.FirstOrDefaultAsync(x => x.UserId == dto.StaffUserId);

            if (staff == null)
            {
                throw new NotFoundException("Không tìm thấy nhân viên.");
            }

            washLog.LaneId = dto.LaneId;
            washLog.StaffUserId = dto.StaffUserId;
            washLog.Status = "Assigned";

            await _context.SaveChangesAsync();
        }

        //public async Task<List<DTOs.Business.TimeSlotResponseDTO>> GetAvailableSlotsForBusinessAsync(int businessUserId, CheckBusinessSlotsRequestDTO request)
        //{
        //    // 1. Verify business + vehicle ownership
        //    var business = await _context.BusinessProfiles
        //        .FirstOrDefaultAsync(x => x.UserId == businessUserId && x.ApprovalStatus == "Approved");

        //    if (business == null)
        //        throw new NotFoundException("Không tìm thấy hồ sơ doanh nghiệp hoặc chưa được phê duyệt.");

        //    var fleetVehicle = await _context.FleetVehicles
        //        .Include(x => x.VehicleType)
        //        .FirstOrDefaultAsync(x =>
        //            x.FleetVehicleId == request.FleetVehicleId &&
        //            x.BusinessProfileId == business.BusinessProfileId &&
        //            x.Status == "Active");

        //    if (fleetVehicle == null)
        //        throw new NotFoundException("Không tìm thấy phương tiện hoặc phương tiện chưa được kích hoạt.");

        //    // 2. Timezone (same cross-platform pattern as customer slot checker)
        //    TimeZoneInfo vnTimeZone;
        //    try { vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
        //    catch (TimeZoneNotFoundException) { vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }

        //    DateTime todayInVN = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone).Date;
        //    TimeSpan currentTimeInVN = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone).TimeOfDay;

        //    // No tier booking window — business can book as far ahead as needed
        //    if (request.TargetDate.Date < todayInVN)
        //        throw new BadRequestException("Không thể đặt lịch cho ngày trong quá khứ.");

        //    // 3. Calculate capacity weight from services (same logic as customer version)
        //    int totalRequestWeight = 0;
        //    if (request.ServiceIds.Any())
        //    {
        //        int baseWeight = fleetVehicle.VehicleType?.BaseWeight ?? 0;

        //        foreach (var serviceId in request.ServiceIds)
        //        {
        //            var servicePrice = await _context.ServicePrices
        //                .FirstOrDefaultAsync(sp =>
        //                    sp.ServiceId == serviceId &&
        //                    sp.VehicleTypeId == fleetVehicle.VehicleTypeId &&
        //                    sp.BranchId == request.BranchId);

        //            // Mirror the same fallback: servicePrice weight → baseWeight
        //            var actualWeight = servicePrice?.CapacityWeight > 0
        //                ? servicePrice.CapacityWeight
        //                : baseWeight;

        //            if (actualWeight > totalRequestWeight)
        //                totalRequestWeight = actualWeight;
        //        }
        //    }
        //    else
        //    {
        //        // No services selected yet — use vehicle base weight as minimum estimate
        //        totalRequestWeight = fleetVehicle.VehicleType?.BaseWeight ?? 0;
        //    }

        //    // 4. Load slots and daily booked weights
        //    var allSlots = await _context.TimeSlots
        //        .Where(s => s.BranchId == request.BranchId)
        //        .OrderBy(s => s.StartTime)
        //        .ToListAsync();

        //    var dailyCapacities = await _context.DailySlotCapacities
        //        .Where(dc => dc.BranchId == request.BranchId && dc.Date == request.TargetDate.Date)
        //        .ToDictionaryAsync(dc => dc.SlotId, dc => dc.BookedWeight);

        //    // 5. Build response — no VIP check needed for business accounts
        //    var response = new List<TimeSlotResponseDTO>();

        //    foreach (var slot in allSlots)
        //    {
        //        var slotDto = new TimeSlotResponseDTO
        //        {
        //            SlotId = slot.SlotId,
        //            TimeRange = $"{slot.StartTime:hh\\:mm} - {slot.EndTime:hh\\:mm}",
        //            IsAvailable = true,
        //            Reason = "Trống"
        //        };

        //        // Past time check
        //        if (request.TargetDate.Date == todayInVN && slot.StartTime < currentTimeInVN)
        //        {
        //            slotDto.IsAvailable = false;
        //            slotDto.Reason = "Đã qua giờ";
        //        }

        //        // Capacity check
        //        int bookedWeight = dailyCapacities.TryGetValue(slot.SlotId, out int weight) ? weight : 0;

        //        if (bookedWeight + totalRequestWeight > slot.MaxCapacity)
        //        {
        //            slotDto.IsAvailable = false;
        //            slotDto.Reason = totalRequestWeight > 0
        //                ? "Không đủ sức chứa cho phương tiện này"
        //                : "Đã kín chỗ";
        //        }

        //        response.Add(slotDto);
        //    }

        //    return response;
        //}
    }
}
