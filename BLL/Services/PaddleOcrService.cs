using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;

namespace BLL.Services
{
    public class PaddleOcrService : IDisposable
    {
        private readonly InferenceSession _detSession;
        private readonly InferenceSession _recSession;
        private readonly ILogger<PaddleOcrService> _logger;
        private readonly string _charSet;

        public PaddleOcrService(string detModelPath, string recModelPath, string dictPath, ILogger<PaddleOcrService> logger)
        {
            _logger = logger;
            var options = new SessionOptions();
            options.GraphOptimizationLevel =
                GraphOptimizationLevel.ORT_ENABLE_ALL;

            _detSession = new InferenceSession(detModelPath, options);
            _recSession = new InferenceSession(recModelPath, options);

            // Load charset from dict.txt
            var chars = File.ReadAllLines(dictPath)
                            .Select(l => l.TrimEnd('\r', '\n'))
                            .ToList();

            _charSet = string.Join("", chars);
            _logger.LogInformation(
                "Charset loaded: {Size} chars. First='{F}' Last='{L}'",
                _charSet.Length,
                _charSet.FirstOrDefault(),
                _charSet.LastOrDefault());
        }

        public async Task<string> ExtractTextAsync(
            byte[] imageBytes,
            string plateType = "SHORT")
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Step 1 — Upscale plate
                    var upscaled = UpscalePlate(imageBytes);

                    // Step 2 — Detect text regions
                    var regions = DetectTextRegions(upscaled);
                    _logger.LogInformation(
                        "Detected {Count} text regions", regions.Count);

                    if (!regions.Any())
                    {
                        _logger.LogWarning("No text regions found");
                        return string.Empty;
                    }

                    // Step 3 — Sort top→bottom, left→right
                    var sorted = regions
                        .OrderBy(r => r.Top)
                        .ThenBy(r => r.Left)
                        .ToList();

                    // Step 4 — Recognize each region
                    var parts = new List<string>();
                    foreach (var region in sorted)
                    {
                        var crop = CropRegion(upscaled, region);
                        var text = RecognizeText(crop);
                        _logger.LogInformation(
                            "Region [{L},{T},{R},{B}] → '{Text}'",
                            region.Left, region.Top,
                            region.Right, region.Bottom,
                            text);

                        if (!string.IsNullOrEmpty(text))
                            parts.Add(text);
                    }

                    var final = string.Join("", parts).ToUpper();
                    _logger.LogInformation("Final plate text: '{Text}'", final);
                    return final;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "PaddleOCR failed: {Msg}", ex.Message);
                    return string.Empty;
                }
            });
        }

        // ── Upscale ──────────────────────────────────────────────────────────
        private byte[] UpscalePlate(byte[] imageBytes)
        {
            using var bitmap = SKBitmap.Decode(imageBytes);

            int scale = Math.Max(4, 400 / Math.Max(bitmap.Width, 1));
            int width = bitmap.Width * scale;
            int height = bitmap.Height * scale;

            _logger.LogInformation(
                "Upscaling {W}x{H} → {NW}x{NH}",
                bitmap.Width, bitmap.Height, width, height);

            using var resized = bitmap.Resize(
                new SKImageInfo(width, height), SKFilterQuality.High);

            // Add white padding
            int pad = 20;
            int rawW = width + pad * 2;
            int rawH = height + pad * 2;

            // ── Round UP to nearest multiple of 32 ──────────────────────────────
            // PaddleOCR det model requires H and W divisible by 32
            int finalW = ((rawW + 31) / 32) * 32;
            int finalH = ((rawH + 31) / 32) * 32;

            _logger.LogInformation(
                "Padded to {W}x{H} (must be multiples of 32)", finalW, finalH);

            using var padded = new SKBitmap(finalW, finalH);
            using (var canvas = new SKCanvas(padded))
            {
                canvas.Clear(SKColors.White);
                canvas.DrawBitmap(resized, pad, pad);
            }

            using var ms = new MemoryStream();
            padded.Encode(ms, SKEncodedImageFormat.Png, 100);

            File.WriteAllBytes(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "debug_upscaled.png"), ms.ToArray());

            return ms.ToArray();
        }

        // ── Detection ────────────────────────────────────────────────────────
        private List<TextRegion> DetectTextRegions(byte[] imageBytes)
        {
            using var bitmap = SKBitmap.Decode(imageBytes);

            // Verify dimensions are multiples of 32
            int H = bitmap.Height;
            int W = bitmap.Width;

            _logger.LogInformation(
                "Det input size: {W}x{H} (div32: {WOk}/{HOk})",
                W, H, W % 32 == 0, H % 32 == 0);

            // Normalize with ImageNet mean/std
            var tensor = new DenseTensor<float>(new[] { 1, 3, H, W });
            float[] mean = { 0.485f, 0.456f, 0.406f };
            float[] std = { 0.229f, 0.224f, 0.225f };

            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    var p = bitmap.GetPixel(x, y);
                    tensor[0, 0, y, x] = (p.Red / 255f - mean[0]) / std[0];
                    tensor[0, 1, y, x] = (p.Green / 255f - mean[1]) / std[1];
                    tensor[0, 2, y, x] = (p.Blue / 255f - mean[2]) / std[2];
                }
            }

            // Input name: "x", Output name: "sigmoid_0.tmp_0"
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("x", tensor)
            };

            using var results = _detSession.Run(inputs);
            var output = results
                .First(r => r.Name == "sigmoid_0.tmp_0")
                .AsTensor<float>();

            return ExtractBoxes(output, W, H, threshold: 0.3f);
        }

        private List<TextRegion> ExtractBoxes(
            Tensor<float> heatmap, int W, int H, float threshold)
        {
            int mapH = heatmap.Dimensions[2];
            int mapW = heatmap.Dimensions[3];

            float scaleX = (float)W / mapW;
            float scaleY = (float)H / mapH;

            bool[,] visited = new bool[mapH, mapW];
            bool[,] isFg = new bool[mapH, mapW];

            for (int y = 0; y < mapH; y++)
                for (int x = 0; x < mapW; x++)
                    isFg[y, x] = heatmap[0, 0, y, x] > threshold;

            var regions = new List<TextRegion>();

            for (int y = 0; y < mapH; y++)
            {
                for (int x = 0; x < mapW; x++)
                {
                    if (!isFg[y, x] || visited[y, x]) continue;

                    // BFS flood fill
                    var queue = new Queue<(int, int)>();
                    var pixels = new List<(int, int)>();
                    queue.Enqueue((y, x));
                    visited[y, x] = true;

                    while (queue.Count > 0)
                    {
                        var (cy, cx) = queue.Dequeue();
                        pixels.Add((cy, cx));

                        foreach (var (dy, dx) in new[]
                            { (0,1),(0,-1),(1,0),(-1,0) })
                        {
                            int ny = cy + dy, nx = cx + dx;
                            if (ny >= 0 && ny < mapH &&
                                nx >= 0 && nx < mapW &&
                                !visited[ny, nx] && isFg[ny, nx])
                            {
                                visited[ny, nx] = true;
                                queue.Enqueue((ny, nx));
                            }
                        }
                    }

                    // Skip noise
                    if (pixels.Count < 10) continue;

                    int minY = pixels.Min(p => p.Item1);
                    int maxY = pixels.Max(p => p.Item1);
                    int minX = pixels.Min(p => p.Item2);
                    int maxX = pixels.Max(p => p.Item2);

                    int pad = 4;
                    regions.Add(new TextRegion
                    {
                        Left = (int)Math.Max(0, (minX - pad) * scaleX),
                        Top = (int)Math.Max(0, (minY - pad) * scaleY),
                        Right = (int)Math.Min(W, (maxX + pad) * scaleX),
                        Bottom = (int)Math.Min(H, (maxY + pad) * scaleY)
                    });
                }
            }

            _logger.LogInformation(
                "Extracted {Count} boxes from heatmap", regions.Count);
            return regions;
        }

        // ── Recognition ──────────────────────────────────────────────────────
        private string RecognizeText(byte[] imageBytes)
        {
            using var bitmap = SKBitmap.Decode(imageBytes);

            // Fixed height 32px, dynamic width
            int recH = 32;
            float ratio = (float)bitmap.Width / Math.Max(bitmap.Height, 1);
            int recW = Math.Max(10, (int)(recH * ratio));

            using var resized = bitmap.Resize(
                new SKImageInfo(recW, recH), SKFilterQuality.High);

            // Normalize: (pixel/255 - 0.5) / 0.5
            var tensor = new DenseTensor<float>(new[] { 1, 3, recH, recW });

            for (int y = 0; y < recH; y++)
            {
                for (int x = 0; x < recW; x++)
                {
                    var p = resized.GetPixel(x, y);
                    tensor[0, 0, y, x] = (p.Red / 255f - 0.5f) / 0.5f;
                    tensor[0, 1, y, x] = (p.Green / 255f - 0.5f) / 0.5f;
                    tensor[0, 2, y, x] = (p.Blue / 255f - 0.5f) / 0.5f;
                }
            }

            // Input name: "x", Output name: "softmax_2.tmp_0"
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("x", tensor)
            };

            using var results = _recSession.Run(inputs);
            var output = results
                .First(r => r.Name == "softmax_2.tmp_0")
                .AsTensor<float>();

            return DecodeCtc(output);
        }

        // ── CTC Decoder ──────────────────────────────────────────────────────
        private string DecodeCtc(Tensor<float> output)
        {
            // Shape: [1, T, 97]
            // PaddleOCR convention:
            //   index 0       = blank token
            //   index 1..95   = dict chars (0-indexed from dict.txt)
            //   index 96      = space (end token)

            int T = output.Dimensions[1];
            int numClasses = output.Dimensions[2]; // 97

            var sb = new System.Text.StringBuilder();
            int lastIdx = -1;

            for (int t = 0; t < T; t++)
            {
                // Argmax
                int maxIdx = 0;
                float maxVal = float.MinValue;

                for (int c = 0; c < numClasses; c++)
                {
                    if (output[0, t, c] > maxVal)
                    {
                        maxVal = output[0, t, c];
                        maxIdx = c;
                    }
                }

                // Skip blank (0) and repeated
                if (maxIdx != 0 && maxIdx != lastIdx)
                {
                    // Shift by 1 → index into charset
                    int charIdx = maxIdx - 1;
                    if (charIdx < _charSet.Length)
                    {
                        sb.Append(_charSet[charIdx]);
                        _logger.LogInformation(
                            "t={T} idx={Idx} → '{Char}'", t, maxIdx, _charSet[charIdx]);
                    }
                }
                lastIdx = maxIdx;
            }

            return sb.ToString();
        }

        // ── Crop region ───────────────────────────────────────────────────────
        private byte[] CropRegion(byte[] imageBytes, TextRegion region)
        {
            using var bitmap = SKBitmap.Decode(imageBytes);

            int x = region.Left;
            int y = region.Top;
            int w = Math.Min(region.Right - region.Left, bitmap.Width - x);
            int h = Math.Min(region.Bottom - region.Top, bitmap.Height - y);

            if (w <= 0 || h <= 0) return imageBytes;

            using var cropped = new SKBitmap(w, h);
            bitmap.ExtractSubset(cropped,
                new SKRectI(x, y, x + w, y + h));

            using var ms = new MemoryStream();
            cropped.Encode(ms, SKEncodedImageFormat.Png, 100);
            return ms.ToArray();
        }

        public void Dispose()
        {
            _detSession?.Dispose();
            _recSession?.Dispose();
        }
    }

    public class TextRegion
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }
    }
}