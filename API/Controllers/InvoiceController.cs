using BLL.Helpers;
using BLL.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/v1/invoice")]
    [Authorize]
    public class InvoiceController : ControllerBase
    {
        private readonly IBusinessBookingService _businessBookingService;

        public InvoiceController(IBusinessBookingService businessBookingService)
        {
            _businessBookingService = businessBookingService;
        }

        [Authorize(Roles = "Business, Manager")]
        [HttpGet("invoices")]
        public async Task<IActionResult> GetInvoices()
        {
            int userId = ClaimHelper.GetUserId(User);

            var result = await _businessBookingService.GetInvoicesAsync(userId);

            return Ok(result);
        }

        [Authorize(Roles = "Business, Manager")]
        [HttpGet("invoices/{invoiceId}")]
        public async Task<IActionResult> GetInvoiceDetail(int invoiceId)
        {
            int userId = ClaimHelper.GetUserId(User);

            var result = await _businessBookingService.GetInvoiceDetailAsync(userId, invoiceId);

            return Ok(result);
        }
    }
}
