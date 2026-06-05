using AutoWashPro.DAL.Entities;
using BLL.DTOs;
using BLL.DTOs.Fleet;
using BLL.Helpers;
using BLL.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/v1/fleet")]
    [Authorize]
    public class FleetController : ControllerBase
    {
        private readonly IFleetService _fleetService;
        private readonly IBusinessBookingService _businessBookingService;

        public FleetController(IFleetService fleetService, IBusinessBookingService businessBookingService)
        {
            _fleetService = fleetService;
            _businessBookingService = businessBookingService;
        }

        // Upload fleet file
        [Authorize(Roles = "Business")]
        [HttpPost("import")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportFleet([FromForm] FleetImportRequestDTO request)
        {
            int userId = ClaimHelper.GetUserId(User);
            var result = await _fleetService.ImportFleetAsync(userId, request.File);
            return Ok(new
            {
                statusCode = 200,
                message = "Fleet imported successfully.",
                data = result
            });
        }

        // View pending vehicles
        [Authorize(Roles = "Business")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingVehicles()
        {
            int userId = ClaimHelper.GetUserId(User);

            var result = await _fleetService.GetPendingVehiclesAsync(userId);

            return Ok(new
            {
                statusCode = 200,
                message = "Success",
                data = result
            });
        }

        // Staff review vehicle
        [Authorize(Roles = "Staff,Manager")]
        [HttpPost("staff/approve/{id}")]
        public async Task<IActionResult> ApproveVehicle(int id)
        {
            await _fleetService.ApproveFleetVehicleAsync(id);

            return Ok(new
            {
                statusCode = 200,
                message = "Fleet vehicle approved."
            });
        }

        // Staff reject vehicle
        [Authorize(Roles = "Staff,Manager")]
        [HttpPost("staff/reject/{id}")]
        public async Task<IActionResult> RejectVehicle(int id, [FromBody] ReviewFleetImportDTO dto)
        {
            await _fleetService.RejectFleetVehicleAsync(id, dto.RejectionReason);

            return Ok(new
            {
                statusCode = 200,
                message = "Fleet vehicle rejected."
            });
        }

        [Authorize(Roles = "Manager,Staff")]
        [HttpGet("fleet/imports")]
        public async Task<IActionResult> GetImports()
        {
            var result = await _fleetService.GetImportBatchesAsync();

            return Ok(new
            {
                statusCode = 200,
                message = "Success",
                data = result
            });
        }

        [Authorize(Roles = "Manager,Staff")]
        [HttpGet("fleet/imports/{batchId}")]
        public async Task<IActionResult> GetImportDetail(int batchId)
        {
            var result = await _fleetService.GetImportBatchDetailAsync(batchId);

            return Ok(new
            {
                statusCode = 200,
                message = "Success",
                data = result
            });
        }

        [Authorize(Roles = "Staff,Manager")]
        [HttpPost("check-in")]
        public async Task<IActionResult> CheckIn(FleetCheckInDTO dto)
        {
            var result =
                await _businessBookingService.CheckInAsync(dto.BookingId);

            return Ok(new
            {
                statusCode = 200,
                message = "Success",
                data = result
            });
        }

        //[Authorize(Roles = "Staff,Manager")]
        //[HttpPost("{id}/complete")]
        //public async Task<IActionResult> Complete(int id)
        //{
        //    await _businessBookingService.CompleteWashAsync(id);

        //    return Ok(new
        //    {
        //        statusCode = 200,
        //        message = "Wash completed."
        //    });
        //}

        [Authorize(Roles = "Staff,Manager")]
        [HttpPost("walk-in")]
        public async Task<IActionResult> WalkIn([FromBody] FleetWalkInDTO dto)
        {
            var result = await _businessBookingService.WalkInAsync(dto);

            return Ok(new
            {
                statusCode = 200,
                message = "Fleet vehicle checked in successfully.",
                data = result
            });
        }

        [Authorize(Roles = "Staff,Manager")]
        [HttpPost("walk-out/{washLogId}")]
        public async Task<IActionResult> WalkOut(int washLogId)
        {
            await _businessBookingService.WalkOutAsync(washLogId);

            return Ok(new
            {
                statusCode = 200,
                message = "Fleet vehicle checked out successfully."
            });
        }

        [Authorize(Roles = "Staff,Manager")]
        [HttpPost("{washLogId}/start-processing")]
        public async Task<IActionResult> StartProcessing(int washLogId, [FromBody] StartFleetWashDTO dto)
        {
            int userId = ClaimHelper.GetUserId(User);

            await _businessBookingService.StartProcessingAsync(washLogId, userId, dto);

            return Ok(new
            {
                statusCode = 200,
                message = "Vehicle moved to processing."
            });
        }

        [Authorize(Roles = "Staff,Manager")]
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentVehicles()
        {
            var result =
                await _businessBookingService.GetCurrentVehiclesAsync();

            return Ok(new
            {
                statusCode = 200,
                message = "Success",
                data = result
            });
        }

        [Authorize(Roles = "Staff,Manager")]
        [HttpGet("queue")]
        public async Task<IActionResult> GetQueue([FromQuery] int branchId)
        {
            var result =
                await _fleetService.GetBusinessQueueAsync(branchId);

            return Ok(new
            {
                statusCode = 200,
                message = "Success",
                data = result
            });
        }

        [Authorize(Roles = "Business")]
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] FleetHistoryFilterDTO filter)
        {
            int userId = ClaimHelper.GetUserId(User);

            var result = await _fleetService.GetHistoryAsync(userId, filter);

            return Ok(new
            {
                statusCode = 200,
                message = "Success",
                data = result
            });
        }

        [Authorize(Roles = "Business")]
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            int userId = ClaimHelper.GetUserId(User);

            var result =
                await _fleetService.GetDashboardAsync(userId);

            return Ok(new
            {
                statusCode = 200,
                message = "Success",
                data = result
            });
        }

        [Authorize(Roles = "Staff,Manager")]
        [HttpPost("checkout/{washLogId}")]
        public async Task<IActionResult> CheckOut(int washLogId)
        {
            var result = await _businessBookingService.CheckOutAsync(washLogId);

            return Ok(new
            {
                statusCode = 200,
                message = "Check out successful.",
                data = result
            });
        }
    }
}