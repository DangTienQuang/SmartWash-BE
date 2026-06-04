using AutoWashPro.DAL.Data;
using BLL.DTOs;
using BLL.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class BookingAttendanceService : IBookingAttendanceService
    {
        private readonly AutoWashDbContext _context;

        public BookingAttendanceService(AutoWashDbContext context)
        {
            _context = context;
        }

        public async Task CheckInVehicleAsync(VehicleCheckInDTO dto)
        {
            var detail = await _context.BookingDetails
                .FirstOrDefaultAsync(x =>
                    x.DetailId == dto.BookingDetailId);

            if (detail == null)
            {
                throw new Exception("Booking detail not found.");
            }

            detail.AttendanceStatus = "CheckedIn";
            detail.CheckInTime = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task CompleteVehicleAsync(VehicleCompleteDTO dto)
        {
            var detail = await _context.BookingDetails
                .FirstOrDefaultAsync(x =>
                    x.DetailId == dto.BookingDetailId);

            if (detail == null)
            {
                throw new Exception("Booking detail not found.");
            }

            detail.AttendanceStatus = "Completed";
            detail.CheckOutTime = DateTime.UtcNow;
            detail.ActualPrice = dto.ActualPrice;
            detail.DepositStatus = "Applied";

            await _context.SaveChangesAsync();
        }

        public async Task MarkNoShowAsync(VehicleNoShowDTO dto)
        {
            var detail = await _context.BookingDetails
                .FirstOrDefaultAsync(x =>
                    x.DetailId == dto.BookingDetailId);

            if (detail == null)
            {
                throw new Exception("Booking detail not found.");
            }

            detail.AttendanceStatus = "NoShow";
            detail.DepositStatus = "Forfeited";

            await _context.SaveChangesAsync();
        }
    }
}
