using AutoWashPro.BLL.Exceptions;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
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

        public BusinessBookingService(AutoWashDbContext context)
        {
            _context = context;
        }

        public async Task<BusinessBookingResponseDTO> CreateBookingAsync(int businessUserId, CreateBusinessBookingDTO dto)
        {
            var business = await _context.BusinessProfiles
                .FirstOrDefaultAsync(x =>
                    x.UserId == businessUserId &&
                    x.ApprovalStatus == "Approved");

            if (business == null)
            {
                throw new NotFoundException("Business profile not found.");
            }

            var fleetVehicle = await _context.FleetVehicles.Include(x => x.VehicleType)
                .FirstOrDefaultAsync(x =>
                    x.FleetVehicleId == dto.FleetVehicleId &&
                    x.BusinessProfileId == business.BusinessProfileId);

            if (fleetVehicle == null)
            {
                throw new NotFoundException("Fleet vehicle not found.");
            }

            if (fleetVehicle.Status != "Active")
            {
                throw new BadRequestException(
                    "Fleet vehicle is not active.");
            }

            var branch = await _context.Branches
                .FirstOrDefaultAsync(x => x.BranchId == dto.BranchId);

            if (branch == null)
            {
                throw new NotFoundException("Branch not found.");
            }

            var slot = await _context.TimeSlots
                .FirstOrDefaultAsync(x => x.SlotId == dto.SlotId && x.BranchId == dto.BranchId);

            if (slot == null)
            {
                throw new NotFoundException("Time slot not found.");
            }

            var scheduledTime = dto.ScheduledTime.Date.Add(slot.StartTime);

            var dailyCapacity = await _context.DailySlotCapacities
                .FirstOrDefaultAsync(x => x.BranchId == dto.BranchId && x.SlotId == dto.SlotId && x.Date == dto.ScheduledTime.Date);

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

            if (dailyCapacity.BookedWeight + fleetVehicle.VehicleType.BaseWeight > slot.MaxCapacity)
            {
                throw new BadRequestException("Slot is full.");
            }

            dailyCapacity.BookedWeight += fleetVehicle.VehicleType.BaseWeight;

            var services = await _context.Services
                .Where(x => dto.ServiceIds.Contains(x.ServiceId))
                .ToListAsync();

            if (services.Count != dto.ServiceIds.Count)
            {
                throw new BadRequestException("One or more services do not exist.");
            }

            decimal totalPrice = 0;

            foreach (var service in services)
            {
                var servicePrice = await _context.ServicePrices
                    .FirstOrDefaultAsync(x =>
                        x.ServiceId == service.ServiceId &&
                        x.VehicleTypeId == fleetVehicle.VehicleTypeId &&
                        x.BranchId == dto.BranchId);

                if (servicePrice == null)
                {
                    throw new BadRequestException($"Price not configured for service {service.ServiceName}");
                }

                totalPrice += servicePrice.Price;
            }

            var booking = new Booking
            {
                BusinessProfileId = business.BusinessProfileId,
                FleetVehicleId = fleetVehicle.FleetVehicleId,
                BookingType = "Business",
                BranchId = dto.BranchId,
                ScheduledTime = scheduledTime,
                LicensePlate = fleetVehicle.LicensePlate,
                Status = "Pending",
                OriginalPrice = totalPrice,
                FinalAmount = totalPrice,
                CapacityWeight = fleetVehicle.VehicleType.BaseWeight,
                FallbackQrCode = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()
            };

            _context.Bookings.Add(booking);

            foreach (var service in services)
            {
                var servicePrice = await _context.ServicePrices
                    .FirstAsync(x =>
                        x.ServiceId == service.ServiceId &&
                        x.VehicleTypeId == fleetVehicle.VehicleTypeId &&
                        x.BranchId == dto.BranchId);

                _context.BookingDetails.Add(new BookingDetail
                {
                    BookingId = booking.BookingId,
                    ServiceId = service.ServiceId,
                    Price = servicePrice.Price
                });
            }

            await _context.SaveChangesAsync();

            return new BusinessBookingResponseDTO
            {
                BookingId = booking.BookingId,
                LicensePlate = booking.LicensePlate,
                OriginalPrice = booking.OriginalPrice,
                FinalAmount = booking.FinalAmount,
                Status = booking.Status
            };
        }

        public async Task<List<FleetVehicleDTO>> GetActiveFleetVehiclesAsync(int businessUserId)
        {
            var business = await _context.BusinessProfiles
                .FirstOrDefaultAsync(x => x.UserId == businessUserId);

            if (business == null) throw new NotFoundException("Business profile not found.");

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

            if (business == null) throw new NotFoundException("Business profile not found.");

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
                throw new NotFoundException("Business profile not found.");
            }

            var booking = await _context.Bookings
                .Include(x => x.BookingDetails)
                    .ThenInclude(x => x.Service)
                .FirstOrDefaultAsync(x =>
                    x.BookingId == bookingId &&
                    x.BusinessProfileId == business.BusinessProfileId);

            if (booking == null)
            {
                throw new NotFoundException("Booking not found.");
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
                throw new NotFoundException("Business profile not found.");
            }

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(x =>
                    x.BookingId == bookingId &&
                    x.BusinessProfileId == business.BusinessProfileId);

            if (booking == null)
            {
                throw new NotFoundException("Booking not found.");
            }

            if (booking.Status != "Pending")
            {
                throw new BadRequestException("Only pending bookings can be cancelled.");
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
                .FirstOrDefaultAsync(x =>
                    x.BookingId == bookingId);

            if (booking == null) throw new NotFoundException("Booking not found.");

            if (booking.BookingType != "Business") throw new BadRequestException("Not a business booking.");

            if (booking.Status != "Pending") throw new BadRequestException("Booking cannot be checked in.");

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

        public async Task CompleteWashAsync(int fleetWashLogId)
        {
            var washLog = await _context.FleetWashLogs
                .Include(x => x.Booking)
                .FirstOrDefaultAsync(x =>
                    x.FleetWashLogId == fleetWashLogId);

            if (washLog == null) throw new NotFoundException("Wash log not found.");

            if (washLog.Status == "Completed") throw new BadRequestException("Already completed.");

            washLog.CompletedTime = DateTime.UtcNow;
            washLog.Status = "Completed";

            washLog.Booking.Status = "Completed";

            await _context.SaveChangesAsync();
        }

        public async Task<FleetCheckInResponseDTO> WalkInAsync(FleetWalkInDTO dto)
        {
            var vehicle = await _context.FleetVehicles
                .FirstOrDefaultAsync(x =>
                    x.LicensePlate == dto.LicensePLate &&
                    x.Status == "Active");

            if (vehicle == null)
            {
                throw new NotFoundException("Fleet vehicle not found.");
            }

            var branch = await _context.Branches
                .FirstOrDefaultAsync(x =>
                    x.BranchId == dto.BranchId);

            if (branch == null)
            {
                throw new NotFoundException("Branch not found.");
            }

            var existingLog = await _context.FleetWashLogs
                .FirstOrDefaultAsync(x =>
                    x.FleetVehicleId == vehicle.FleetVehicleId &&
                    (x.Status == "CheckedIn" ||
                     x.Status == "Processing"));

            if (existingLog != null)
            {
                throw new BadRequestException("Vehicle is already in washing process.");
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
                .FirstOrDefaultAsync(x =>
                    x.FleetWashLogId == washLogId);

            if (washLog == null)
            {
                throw new NotFoundException("Wash log not found.");
            }

            if (washLog.Status == "Completed")
            {
                throw new BadRequestException("Vehicle already checked out.");
            }

            washLog.CompletedTime = DateTime.UtcNow;
            washLog.Status = "Completed";

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
                throw new NotFoundException("Wash log not found.");
            }

            if (washLog.Status != "CheckedIn")
            {
                throw new BadRequestException("Vehicle is not waiting for processing.");
            }

            var lane = await _context.Lanes
                .FirstOrDefaultAsync(x => x.LaneId == dto.LaneId);

            if (lane == null)
            {
                throw new NotFoundException("Lane not found.");
            }

            washLog.Status = "Processing";

            if (washLog.Booking != null)
            {
                washLog.Booking.ProcessingLaneId = dto.LaneId;
                washLog.Booking.ProcessingStaffId = staffUserId;
            }

            await _context.SaveChangesAsync();
        }
    }
}
