using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AutoWashPro.API.Controllers
{
    [Route("api/v1/admin/vehicle-types")]
    [ApiController]
    [Authorize(Roles = "Admin")] 
    public class AdminVehicleTypeController : ControllerBase
    {
        private readonly IVehicleTypeService _typeService;

        public AdminVehicleTypeController(IVehicleTypeService typeService)
        {
            _typeService = typeService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVehicleTypeDTO request)
        {
            try
            {
                var result = await _typeService.CreateAsync(request);
                return Created("", new { statusCode = 201, message = "Thêm loại xe thành công.", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateVehicleTypeDTO request)
        {
            try
            {
                await _typeService.UpdateAsync(id, request);
                return Ok(new { statusCode = 200, message = "Cập nhật loại xe thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _typeService.DeleteAsync(id);
                return Ok(new { statusCode = 200, message = "Xóa loại xe thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await _typeService.GetAllAsync();
                return Ok(new { statusCode = 200, message = "Success", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }
    }
}