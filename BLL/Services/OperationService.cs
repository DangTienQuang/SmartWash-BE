using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoWashPro.BLL.DTOs;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoWashPro.BLL.Services
{
    public class OperationService : IOperationService
    {
        private readonly AutoWashDbContext _context;

        public OperationService(AutoWashDbContext context)
        {
            _context = context;
        }

        public async Task<List<ServiceResponseDTO>> GetServicesAsync()
        {
            return await _context.Services.Select(s => new ServiceResponseDTO
            {
                ServiceId = s.ServiceId,
                ServiceName = s.ServiceName,
                BasePrice = s.BasePrice,
                DurationMinutes = s.DurationMinutes
            }).ToListAsync();
        }

        public async Task<BookingResponseDTO> CreateBookingAsync(int userId, CreateBookingDTO request)
        {
            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.LicensePlate == request.LicensePlate && v.UserId == userId);
            if (vehicle == null) throw new Exception("Vehicle not found or does not belong to user.");

            var service = await _context.Services.FindAsync(request.ServiceId);
            if (service == null) throw new Exception("Service not found.");

            var userProfile = await _context.CustomerProfiles
                .Include(cp => cp.Tier)
                .FirstOrDefaultAsync(cp => cp.UserId == userId);

            if (userProfile == null || userProfile.Tier == null) throw new Exception("Profile or Tier not found.");

            var maxDate = DateTime.UtcNow.AddDays(userProfile.Tier.BookingWindowDays);
            if (request.ScheduledTime <= DateTime.UtcNow || request.ScheduledTime > maxDate)
                throw new Exception($"Booking time must be between now and {maxDate:yyyy-MM-dd HH:mm}.");

            var booking = new Booking
            {
                UserId = userId,
                LicensePlate = request.LicensePlate,
                ServiceId = request.ServiceId,
                ScheduledTime = request.ScheduledTime,
                Status = "Pending"
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return new BookingResponseDTO
            {
                BookingId = booking.BookingId,
                LicensePlate = booking.LicensePlate,
                ServiceName = service.ServiceName,
                ScheduledTime = booking.ScheduledTime,
                Status = booking.Status
            };
        }

        public async Task<List<BookingResponseDTO>> GetMyBookingsAsync(int userId)
        {
            return await _context.Bookings
                .Include(b => b.Service)
                .Where(b => b.UserId == userId)
                .Select(b => new BookingResponseDTO
                {
                    BookingId = b.BookingId,
                    LicensePlate = b.LicensePlate,
                    ServiceName = b.Service.ServiceName,
                    ScheduledTime = b.ScheduledTime,
                    Status = b.Status
                }).ToListAsync();
        }
        public async Task<ServiceResponseDTO> CreateServiceAsync(CreateServiceDTO request)
        {
            var service = new Service
            {
                ServiceName = request.ServiceName,
                BasePrice = request.BasePrice,
                DurationMinutes = request.DurationMinutes
            };

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            return new ServiceResponseDTO
            {
                ServiceId = service.ServiceId,
                ServiceName = service.ServiceName,
                BasePrice = service.BasePrice,
                DurationMinutes = service.DurationMinutes
            };
        }
    }
}