using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoWashPro.API.Controllers
{
    [ApiController]
    [Route("api/v1/wallets")]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMyWallet()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null) return Unauthorized(new { statusCode = 401, message = "Unauthorized" });

                var userId = int.Parse(userIdClaim);
                var result = await _walletService.GetWalletInfoAsync(userId);
                return Ok(new { statusCode = 200, message = "Success", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("top-up")]
        public async Task<IActionResult> TopUp([FromBody] TopUpRequestDTO request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null) return Unauthorized(new { statusCode = 401, message = "Unauthorized" });

                var userId = int.Parse(userIdClaim);
                var result = await _walletService.CreateTopUpLinkAsync(userId, request);
                return Ok(new { statusCode = 200, message = "Success", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }

        
        /// <remarks>
        /// Mẫu JSON tối giản để test nhanh trên Swagger:
        /// 
        /// {
        ///   "code": "00",
        ///   "data": {
        ///     "orderCode": 12345678,
        ///     "amount": 50000,
        ///     "description": "Topup wallet 2"
        ///   }
        /// }
        /// </remarks>
        [HttpPost("top-up/callback")]
        public async Task<IActionResult> PayOSWebhook([FromBody] WebhookTopUpDTO webhookData)
        {
            try
            {
                await _walletService.ProcessPaymentWebhookAsync(webhookData);
                return Ok(new { statusCode = 200, message = "Success" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }
    }
}
