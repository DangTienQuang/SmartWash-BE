using AutoWashPro.BLL.Exceptions;
using BLL.DTOs;
using BLL.DTOs.Business;
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
        private readonly IFleetService _fleetService;
        private readonly IBusinessBookingService _businessBookingService;

        public BusinessController(IBusinessService businessService, IInvoiceService invoiceService, IBookingAttendanceService attendanceService, IFleetService fleetService, IBusinessBookingService businessBookingService)
        {
            _businessService = businessService;
            _invoiceService = invoiceService;
            _attendanceService = attendanceService;
            _fleetService = fleetService;
            _businessBookingService = businessBookingService;
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

        [Authorize(Roles = "Staff,Manager")]
        [HttpPost("staff/review-application")]
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

        [Authorize(Roles = "Business")]
        [HttpPost("api/v1/business-bookings")]
        public async Task<IActionResult> CreateBooking(CreateBusinessBookingDTO dto)
        {
            int userId = ClaimHelper.GetUserId(User);

            var result = await _businessBookingService.CreateBookingAsync(userId, dto);

            return Ok(new
            {
                statusCode = 200,
                message = "Success",
                data = result
            });
        }

        [Authorize(Roles = "Business")]
        [HttpGet("vehicles")]
        public async Task<IActionResult> GetActiveVehicles()
        {
            int userId = ClaimHelper.GetUserId(User);

            var result = await _businessBookingService.GetActiveFleetVehiclesAsync(userId);

            return Ok(new
            {
                statusCode = 200,
                message = "Success",
                data = result
            });
        }

        [Authorize(Roles = "Business")]
        [HttpGet]
        public async Task<IActionResult> GetBookings()
        {
            int userId = ClaimHelper.GetUserId(User);

            var result = await _businessBookingService.GetBookingsAsync(userId);

            return Ok(new
            {
                statusCode = 200,
                message = "Success",
                data = result
            });
        }

        [Authorize(Roles = "Business")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookingDetail(int id)
        {
            int userId = ClaimHelper.GetUserId(User);

            var result =await _businessBookingService.GetBookingDetailAsync(userId, id);

            return Ok(new
            {
                statusCode = 200,
                message = "Success",
                data = result
            });
        }

        [Authorize(Roles = "Business")]
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelBooking(int id)
        {
            int userId = ClaimHelper.GetUserId(User);

            await _businessBookingService.CancelBookingAsync(userId, id);

            return Ok(new
            {
                statusCode = 200,
                message = "Booking cancelled successfully."
            });
        }
    }
}