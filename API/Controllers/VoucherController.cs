using System;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoWashPro.API.Controllers
{
    [ApiController]
    [Route("api/v1/vouchers")]
    public class VoucherController : ControllerBase
    {
        private readonly IVoucherService _voucherService;

        public VoucherController(IVoucherService voucherService)
        {
            _voucherService = voucherService;
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMyVouchers()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var result = await _voucherService.GetMyVouchersAsync(userId);
                return Ok(new { statusCode = 200, message = "Success", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("redeem")]
        public async Task<IActionResult> RedeemVoucher([FromBody] RedeemVoucherRequestDTO request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                await _voucherService.RedeemVoucherAsync(userId, request.VoucherId);
                return Ok(new { statusCode = 200, message = "Đổi voucher thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }
    }
}
