using BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehicleDetectionController : ControllerBase
    {
        private readonly ILicensePlateService _plateService;

        public VehicleDetectionController(ILicensePlateService plateService)
        {
            _plateService = plateService;
        }

        // Single image endpoint
        [HttpPost("detect-plate")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> DetectPlate(IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest(new { message = "No image provided." });

            using var ms = new MemoryStream();
            await image.CopyToAsync(ms);

            var result = await _plateService.DetectPlateAsync(ms.ToArray());

            if (!result.Detected)
                return NotFound(new { message = "No license plate detected." });

            return Ok(new
            {
                plateText = result.PlateText,
                confidence = result.Confidence
            });
        }

        // Dual image endpoint
        [HttpPost("detect-dual-plate")]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> DetectDualPlate(
            IFormFile? frontImage,
            IFormFile? backImage)
        {
            if (frontImage == null && backImage == null)
                return BadRequest(new { message = "At least one image required." });

            byte[]? frontBytes = null;
            byte[]? backBytes = null;

            if (frontImage != null)
            {
                using var ms = new MemoryStream();
                await frontImage.CopyToAsync(ms);
                frontBytes = ms.ToArray();
            }

            if (backImage != null)
            {
                using var ms = new MemoryStream();
                await backImage.CopyToAsync(ms);
                backBytes = ms.ToArray();
            }

            var result = await _plateService.DetectDualPlateAsync(frontBytes, backBytes);

            if (!result.Detected)
                return NotFound(new { message = "No license plate detected." });

            return Ok(new
            {
                plateText = result.FinalPlateText,
                confirmedBy = result.ConfirmedBy,
                front = result.Front == null ? null : new
                {
                    detected = result.Front.Detected,
                    plateText = result.Front.PlateText,
                    confidence = result.Front.Confidence,
                    plateType = result.Front.PlateType
                },
                back = result.Back == null ? null : new
                {
                    detected = result.Back.Detected,
                    plateText = result.Back.PlateText,
                    confidence = result.Back.Confidence,
                    plateType = result.Back.PlateType
                }
            });
        }
    }
}