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

        public BusinessController(
            IBusinessService businessService,
            IInvoiceService invoiceService,
            IBookingAttendanceService attendanceService)
        {
            _businessService = businessService;
            _invoiceService = invoiceService;
            _attendanceService = attendanceService;
        }

        [Authorize(Roles = "Customer")]
        [HttpPost("register")]
        public async Task<IActionResult> RegisterBusiness([FromForm] CreateBusinessProfileRequest request)
        {
            if (request == null)
                throw new BadRequestException("Request body is required.");

            int userId = ClaimHelper.GetUserId(User);

            var result = await _businessService.CreateBusinessProfileAsync(userId, request);

            return Ok(new
            {
                message = "Business registration submitted successfully. Waiting for approval.",
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
                throw new NotFoundException("Business profile not found.");

            return Ok(new
            {
                statusCode = 200,
                message = "Success",
                data = result
            });
        }

        [Authorize(Roles = "Business")]
        [HttpPost("bookings")]
        public async Task<IActionResult> CreateBusinessBooking([FromBody] CreateBusinessBookingDTO dto)
        {
            if (dto == null)
                throw new BadRequestException("Booking data is required.");

            int userId = ClaimHelper.GetUserId(User);

            var bookingId = await _businessService.CreateBusinessBookingAsync(userId, dto);

            return Ok(new
            {
                statusCode = 200,
                message = "Business booking created successfully.",
                data = new { bookingId }
            });
        }

        [Authorize(Roles = "Business")]
        [HttpGet("bookings")]
        public async Task<IActionResult> GetMyBookings()
        {
            int userId = ClaimHelper.GetUserId(User);

            var result = await _businessService.GetBusinessBookingsAsync(userId);

            return Ok(new
            {
                statusCode = 200,
                message = "Success",
                data = result
            });
        }

        [Authorize(Roles = "Staff")]
        [HttpPost("staff/check-in")]
        public async Task<IActionResult> CheckInVehicle([FromBody] VehicleCheckInDTO dto)
        {
            if (dto == null)
                throw new BadRequestException("Check-in data is required.");

            await _attendanceService.CheckInVehicleAsync(dto);

            return Ok(new
            {
                statusCode = 200,
                message = "Vehicle checked in successfully."
            });
        }

        [Authorize(Roles = "Staff")]
        [HttpPost("staff/complete")]
        public async Task<IActionResult> CompleteVehicle([FromBody] VehicleCompleteDTO dto)
        {
            if (dto == null)
                throw new BadRequestException("Completion data is required.");

            await _attendanceService.CompleteVehicleAsync(dto);

            return Ok(new
            {
                statusCode = 200,
                message = "Vehicle completed successfully."
            });
        }

        [Authorize(Roles = "Staff")]
        [HttpPost("staff/no-show")]
        public async Task<IActionResult> MarkNoShow([FromBody] VehicleNoShowDTO dto)
        {
            if (dto == null)
                throw new BadRequestException("No-show data is required.");

            await _attendanceService.MarkNoShowAsync(dto);

            return Ok(new
            {
                statusCode = 200,
                message = "Vehicle marked as no-show."
            });
        }

        [Authorize(Roles = "Business,Staff")]
        [HttpPost("staff/generate-invoice")]
        public async Task<IActionResult> GenerateInvoice([FromBody] CreateInvoiceDTO dto)
        {
            if (dto == null)
                throw new BadRequestException("Invoice data is required.");

            var result = await _invoiceService.GenerateInvoiceAsync(dto);

            return Ok(new
            {
                statusCode = 200,
                message = "Invoice generated successfully.",
                data = result
            });
        }

        [Authorize(Roles = "Staff,Manager")]
        [HttpPost("staff/review")]
        public async Task<IActionResult> ReviewBusinessProfile([FromBody] ReviewBusinessProfileDTO dto)
        {
            if (dto == null)
                throw new BadRequestException("Review data is required.");

            int reviewerId = ClaimHelper.GetUserId(User);

            await _businessService.ReviewBusinessProfileAsync(reviewerId, dto);

            return Ok(new
            {
                statusCode = 200,
                message = "Business registration reviewed."
            });
        }

        [Authorize(Roles = "Staff,Manager")]
        [HttpGet("staff/pending-applications")]
        public async Task<IActionResult> GetPendingApplications()
        {
            var result = await _businessService.GetPendingBusinessApplicationsAsync();

            return Ok(new
            {
                statusCode = 200,
                message = "Success",
                data = result
            });
        }

        [Authorize(Roles = "Staff,Manager")]
        [HttpGet("staff/application/{businessProfileId}")]
        public async Task<IActionResult> GetApplicationDetail(int businessProfileId)
        {
            if (businessProfileId <= 0)
                throw new BadRequestException("Invalid business profile ID.");

            var result = await _businessService.GetBusinessApplicationDetailAsync(businessProfileId);

            if (result == null)
                throw new NotFoundException($"Application with ID {businessProfileId} not found.");

            return Ok(new
            {
                statusCode = 200,
                message = "Success",
                data = result
            });
        }
    }
}