using DAL.Data;
using DAL.DTOs;
using DAL.Entities;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace BLL.Services
{
    public class LicensePlateService : ILicensePlateService
    {
        private readonly OnnxInferenceEngine _engine;
        private readonly PaddleOcrService _ocr;
        private const float ConfidenceThreshold = 0.5f;
        private const float IouThreshold = 0.45f;
        private const int ModelInputSize = 640;
        private readonly ILogger<LicensePlateService> _logger;

        public LicensePlateService(OnnxInferenceEngine engine, PaddleOcrService ocr, ILogger<LicensePlateService> logger)
        {
            _engine = engine;
            _ocr = ocr;
            _logger = logger;
        }

        // ─── Dual Camera Entry Point ────────────────────────────────────────
        public async Task<DualPlateResult> DetectDualPlateAsync(
            byte[]? frontImageBytes,
            byte[]? backImageBytes)
        {
            // Run both cameras in parallel
            var frontTask = frontImageBytes != null
                ? DetectSingleAsync(frontImageBytes, PlatePosition.Front)
                : Task.FromResult<SinglePlateResult?>(null);

            var backTask = backImageBytes != null
                ? DetectSingleAsync(backImageBytes, PlatePosition.Back)
                : Task.FromResult<SinglePlateResult?>(null);

            await Task.WhenAll(frontTask, backTask);

            var front = await frontTask;
            var back = await backTask;

            return ReconcileResults(front, back);
        }

        // ─── Single Camera Detection ────────────────────────────────────────
        private async Task<SinglePlateResult?> DetectSingleAsync(
            byte[] imageBytes,
            PlatePosition position)
        {
            var boxes = GetFilteredBoxes(imageBytes);
            if (!boxes.Any())
                return new SinglePlateResult
                {
                    Detected = false,
                    Position = position
                };

            var bestBox = boxes.OrderByDescending(b => b.Confidence).First();
            var cropped = CropRegion(imageBytes, bestBox);
            var plateType = DetectPlateType(cropped);
            var plateText = await _ocr.ExtractTextAsync(cropped, plateType);

            return new SinglePlateResult
            {
                Detected = true,
                PlateText = plateText,
                Confidence = bestBox.Confidence,
                Position = position,
                PlateType = plateType
            };
        }

        // ─── Plate Type Detection ───────────────────────────────────────────
        private string DetectPlateType(byte[] croppedBytes)
        {
            using var bitmap = SKBitmap.Decode(croppedBytes);
            float ratio = (float)bitmap.Width / bitmap.Height;

            return ratio >= 3.0f ? "LONG" : "SHORT";
        }

        // ─── Reconcile Front + Back Results ────────────────────────────────
        private DualPlateResult ReconcileResults(
            SinglePlateResult? front,
            SinglePlateResult? back)
        {
            bool frontOk = front?.Detected == true
                           && !string.IsNullOrEmpty(front.PlateText);
            bool backOk = back?.Detected == true
                           && !string.IsNullOrEmpty(back.PlateText);

            // Both detected — compare and pick best
            if (frontOk && backOk)
            {
                // Normalize for comparison (remove dots, spaces)
                var frontNorm = Normalize(front!.PlateText);
                var backNorm = Normalize(back!.PlateText);

                // If they match — high confidence result
                if (frontNorm == backNorm)
                    return new DualPlateResult
                    {
                        Detected = true,
                        FinalPlateText = front.PlateText,
                        ConfirmedBy = "BOTH",
                        Front = front,
                        Back = back
                    };

                // They don't match — prefer back plate
                // (back plate is usually cleaner / less obstructed)
                // But pick whichever has higher YOLO confidence
                var better = front.Confidence >= back.Confidence ? front : back;
                return new DualPlateResult
                {
                    Detected = true,
                    FinalPlateText = better.PlateText,
                    ConfirmedBy = better.Position.ToString(),
                    Front = front,
                    Back = back
                };
            }

            // Only front detected
            if (frontOk)
                return new DualPlateResult
                {
                    Detected = true,
                    FinalPlateText = front!.PlateText,
                    ConfirmedBy = "FRONT",
                    Front = front,
                    Back = back
                };

            // Only back detected
            if (backOk)
                return new DualPlateResult
                {
                    Detected = true,
                    FinalPlateText = back!.PlateText,
                    ConfirmedBy = "BACK",
                    Front = front,
                    Back = back
                };

            // Neither detected
            return new DualPlateResult
            {
                Detected = false,
                Front = front,
                Back = back
            };
        }

        private string Normalize(string plateText)
            => new string(plateText
                .ToUpper()
                .Where(char.IsLetterOrDigit)
                .ToArray());

        // ─── Existing Single Image API (unchanged) ──────────────────────────
        public async Task<LicensePlateResult> DetectPlateAsync(byte[] imageBytes)
        {
            var boxes = GetFilteredBoxes(imageBytes);
            if (!boxes.Any())
                return new LicensePlateResult { Detected = false };

            var bestBox = boxes.OrderByDescending(b => b.Confidence).First();
            var cropped = CropRegion(imageBytes, bestBox);
            var plateType = DetectPlateType(cropped);
            var plateText = await _ocr.ExtractTextAsync(cropped, plateType);

            return new LicensePlateResult
            {
                Detected = true,
                PlateText = plateText,
                Confidence = bestBox.Confidence,
                Boxes = boxes
            };
        }

        // ─── Shared Helpers ─────────────────────────────────────────────────
        private List<DetectionBox> GetFilteredBoxes(byte[] imageBytes)
        {
            var rawOutput = _engine.RunInference(imageBytes);
            var boxes = ParseYoloOutput(rawOutput);
            return ApplyNMS(boxes, IouThreshold);
        }

        private List<DetectionBox> ParseYoloOutput(float[] output)
        {
            var boxes = new List<DetectionBox>();
            int numDetections = 8400;

            for (int i = 0; i < numDetections; i++)
            {
                float conf = output[4 * numDetections + i];
                if (conf < ConfidenceThreshold) continue;

                float cx = output[0 * numDetections + i];
                float cy = output[1 * numDetections + i];
                float w = output[2 * numDetections + i];
                float h = output[3 * numDetections + i];

                boxes.Add(new DetectionBox
                {
                    X1 = (cx - w / 2) / ModelInputSize,
                    Y1 = (cy - h / 2) / ModelInputSize,
                    X2 = (cx + w / 2) / ModelInputSize,
                    Y2 = (cy + h / 2) / ModelInputSize,
                    Confidence = conf
                });
            }
            return boxes;
        }

        private List<DetectionBox> ApplyNMS(
            List<DetectionBox> boxes, float iouThreshold)
        {
            var sorted = boxes.OrderByDescending(b => b.Confidence).ToList();
            var kept = new List<DetectionBox>();

            while (sorted.Any())
            {
                var best = sorted[0];
                kept.Add(best);
                sorted.RemoveAt(0);
                sorted.RemoveAll(b => ComputeIoU(best, b) > iouThreshold);
            }
            return kept;
        }

        private float ComputeIoU(DetectionBox a, DetectionBox b)
        {
            float ix1 = Math.Max(a.X1, b.X1), iy1 = Math.Max(a.Y1, b.Y1);
            float ix2 = Math.Min(a.X2, b.X2), iy2 = Math.Min(a.Y2, b.Y2);
            float inter = Math.Max(0, ix2 - ix1) * Math.Max(0, iy2 - iy1);
            float areaA = (a.X2 - a.X1) * (a.Y2 - a.Y1);
            float areaB = (b.X2 - b.X1) * (b.Y2 - b.Y1);
            return inter / (areaA + areaB - inter);
        }

        private byte[] CropRegion(byte[] imageBytes, DetectionBox box)
        {
            using var bitmap = SKBitmap.Decode(imageBytes);

            // Expand box by 8% on each side
            float expandX = (box.X2 - box.X1) * 0.08f;
            float expandY = (box.Y2 - box.Y1) * 0.08f;

            int x = (int)((box.X1 - expandX) * bitmap.Width);
            int y = (int)((box.Y1 - expandY) * bitmap.Height);
            int w = (int)((box.X2 - box.X1 + expandX * 2) * bitmap.Width);
            int h = (int)((box.Y2 - box.Y1 + expandY * 2) * bitmap.Height);

            x = Math.Max(0, x);
            y = Math.Max(0, y);
            w = Math.Min(w, bitmap.Width - x);
            h = Math.Min(h, bitmap.Height - y);

            _logger.LogInformation(
                "Cropping plate region: x={X} y={Y} w={W} h={H}", x, y, w, h);

            using var cropped = new SKBitmap(w, h);
            bitmap.ExtractSubset(cropped, new SKRectI(x, y, x + w, y + h));

            using var ms = new MemoryStream();
            cropped.Encode(ms, SKEncodedImageFormat.Png, 100);
            return ms.ToArray();
        }
    }
}