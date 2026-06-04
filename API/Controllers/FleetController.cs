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

        public FleetController(IFleetService fleetService)
        {
            _fleetService = fleetService;
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
    }
}