using AutoWashPro.BLL.Exceptions;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using BLL.DTOs;
using BLL.Services.Interface;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services
{
    public class BusinessService : IBusinessService
    {
        private readonly AutoWashDbContext _context;

        public BusinessService(AutoWashDbContext context)
        {
            _context = context;
        }

        public async Task<BusinessProfileResponseDTO> CreateBusinessProfileAsync(int userId, CreateBusinessProfileDTO dto)
        {
            // Find user
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (user == null)
            {
                throw new NotFoundException(
                    "User not found.");
            }

            // Prevent duplicate business registration
            var existingBusiness = await _context.BusinessProfiles
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (existingBusiness != null)
            {
                throw new BadRequestException(
                    "Business profile already exists.");
            }

            // Create business profile
            var entity = new BusinessProfile
            {
                UserId = userId,
                CompanyName = dto.CompanyName,
                TaxCode = dto.TaxCode,
                BusinessAddress = dto.BusinessAddress,
                BillingEmail = dto.BillingEmail,
                RepresentativeName = dto.RepresentativeName,
                PaymentTermDays = dto.PaymentTermDays,
                CreatedAt = DateTime.UtcNow
            };

            _context.BusinessProfiles.Add(entity);

            // Change role
            user.Role = "Business";

            await _context.SaveChangesAsync();

            return new BusinessProfileResponseDTO
            {
                BusinessProfileId = entity.BusinessProfileId,
                CompanyName = entity.CompanyName,
                TaxCode = entity.TaxCode,
                BusinessAddress = entity.BusinessAddress
            };
        }

        public async Task<BusinessProfileResponseDTO?>
            GetByUserIdAsync(int userId)
        {
            return await _context.BusinessProfiles
                .Where(x => x.UserId == userId)
                .Select(x => new BusinessProfileResponseDTO
                {
                    BusinessProfileId = x.BusinessProfileId,
                    CompanyName = x.CompanyName,
                    TaxCode = x.TaxCode,
                    BusinessAddress = x.BusinessAddress
                })
                .FirstOrDefaultAsync();
        }

        public async Task<List<BusinessBookingResponseDTO>> GetBusinessBookingsAsync(int userId)
        {
            var businessProfile = await _context.BusinessProfiles
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (businessProfile == null)
            {
                throw new NotFoundException(
                    "Business profile not found.");
            }

            var bookings = await _context.Bookings
                .Include(x => x.BookingDetails)
                    .ThenInclude(d => d.Service)
                .Where(x =>
                    x.BusinessProfileId ==
                    businessProfile.BusinessProfileId)
                .OrderByDescending(x => x.ScheduledTime)
                .ToListAsync();

            return bookings.Select(x =>
                new BusinessBookingResponseDTO
                {
                    BookingId = x.BookingId,

                    BookingDate = x.ScheduledTime,

                    BookingStatus = x.Status,

                    TotalAmount = x.FinalAmount,

                    TotalVehicles = x.BookingDetails.Count,

                    Vehicles = x.BookingDetails.Select(d =>
                        new BusinessBookingDetailDTO
                        {
                            DetailId = d.DetailId,

                            LicensePlate = d.LicensePlate,

                            ServiceName = d.Service.ServiceName,

                            AttendanceStatus = d.AttendanceStatus,

                            Price = d.Price,

                            DepositAmount = d.DepositAmount,

                            DepositStatus = d.DepositStatus
                        })
                        .ToList()
                })
                .ToList();
        }

        public async Task<int> CreateBusinessBookingAsync(int userId, CreateBusinessBookingDTO dto)
        {
            var businessProfile = await _context.BusinessProfiles
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (businessProfile == null)
            {
                throw new NotFoundException(
                    "Business profile not found.");
            }

            if (dto.Vehicles == null || !dto.Vehicles.Any())
            {
                throw new BadRequestException(
                    "Vehicle list cannot be empty.");
            }

            decimal totalOriginalPrice =
                dto.Vehicles.Sum(x => x.Price);

            // Create booking
            var booking = new Booking
            {
                UserId = userId,

                BusinessProfileId =
                    businessProfile.BusinessProfileId,

                BookingType = "Business",

                ScheduledTime = dto.ScheduledTime,

                Status = "Pending",

                OriginalPrice = totalOriginalPrice,

                FinalAmount = totalOriginalPrice,

                CreatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);

            await _context.SaveChangesAsync();

            // Create booking details
            var bookingDetails =
                dto.Vehicles.Select(v =>
                    new BookingDetail
                    {
                        BookingId = booking.BookingId,

                        LicensePlate = v.LicensePlate,

                        ServiceId = v.ServiceId,

                        Price = v.Price,

                        DepositAmount = v.DepositAmount,

                        DepositStatus = "Reserved",

                        AttendanceStatus = "Pending"
                    })
                .ToList();

            _context.BookingDetails.AddRange(bookingDetails);

            await _context.SaveChangesAsync();

            return booking.BookingId;
        }
    }
}