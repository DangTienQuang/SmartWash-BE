using AutoWashPro.BLL.Exceptions;
using BLL.DTOs;
using BLL.Helpers;
using BLL.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/v1/business")]
    [Authorize]
    public class BusinessController : ControllerBase
    {
        private readonly IBusinessService _businessService;
        private readonly IInvoiceService _invoiceService;
        private readonly IBookingAttendanceService _attendanceService;

        public BusinessController(IBusinessService businessService, IInvoiceService invoiceService, IBookingAttendanceService attendanceService)
        {
            _businessService = businessService;
            _invoiceService = invoiceService;
            _attendanceService = attendanceService;
        }


        [Authorize(Roles = "Customer")]
        [HttpPost("register")]
        public async Task<IActionResult> RegisterBusiness([FromBody] CreateBusinessProfileDTO dto)
        {
            int userId = ClaimHelper.GetUserId(User);

            var result = await _businessService.CreateBusinessProfileAsync(userId, dto);

            return Ok(new
            {
                message = "Business profile created successfully.",
                data = result
            });
        }


        [Authorize(Roles = "Business")]
        [HttpGet("my-profile")]
        public async Task<IActionResult> GetMyBusinessProfile()
        {
            int userId = ClaimHelper.GetUserId(User);

            var result = await _businessService.GetByUserIdAsync(userId);

            if (result == null)
            {
                throw new NotFoundException("Business profile not found.");
            }

            return Ok(result);
        }


        [Authorize(Roles = "Business")]
        [HttpPost("bookings")]
        public async Task<IActionResult> CreateBusinessBooking(
            [FromBody] CreateBusinessBookingDTO dto)
        {
            int userId = ClaimHelper.GetUserId(User);

            var bookingId = await _businessService.CreateBusinessBookingAsync(userId, dto);

            return Ok(new
            {
                message = "Business booking created successfully.",
                bookingId = bookingId
            });
        }


        [Authorize(Roles = "Business")]
        [HttpGet("bookings")]
        public async Task<IActionResult> GetMyBookings()
        {
            int userId = ClaimHelper.GetUserId(User);

            var result = await _businessService.GetBusinessBookingsAsync(userId);

            return Ok(result);
        }


        [Authorize(Roles = "Staff")]
        [HttpPost("check-in")]
        public async Task<IActionResult> CheckInVehicle([FromBody] VehicleCheckInDTO dto)
        {
            await _attendanceService
                .CheckInVehicleAsync(dto);

            return Ok(new
            {
                message = "Vehicle checked in successfully."
            });
        }


        [Authorize(Roles = "Staff")]
        [HttpPost("complete")]
        public async Task<IActionResult> CompleteVehicle([FromBody] VehicleCompleteDTO dto)
        {
            await _attendanceService
                .CompleteVehicleAsync(dto);

            return Ok(new
            {
                message = "Vehicle completed successfully."
            });
        }


        [Authorize(Roles = "Staff")]
        [HttpPost("no-show")]
        public async Task<IActionResult> MarkNoShow([FromBody] VehicleNoShowDTO dto)
        {
            await _attendanceService
                .MarkNoShowAsync(dto);

            return Ok(new
            {
                message = "Vehicle marked as no-show."
            });
        }

        [Authorize(Roles = "Business,Staff")]
        [HttpPost("generate-invoice")]
        public async Task<IActionResult> GenerateInvoice([FromBody] CreateInvoiceDTO dto)
        {
            var result = await _invoiceService
                .GenerateInvoiceAsync(dto);

            return Ok(new
            {
                message = "Invoice generated successfully.",
                data = result
            });
        }
    }
}