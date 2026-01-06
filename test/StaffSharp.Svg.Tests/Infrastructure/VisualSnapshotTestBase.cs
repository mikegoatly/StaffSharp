namespace StaffSharp.Svg.Tests.Infrastructure;

using System.IO;
using System.Runtime.CompilerServices;

using global::SkiaSharp;

using Xunit;

/// <summary>
/// Base class for visual snapshot tests that render SVG and compare against golden images.
///
/// Usage:
/// 1. First run: Test generates actual.png and saves to Snapshots/{testName}.png for review
/// 2. Subsequent runs: Test compares against golden image with configurable tolerance
/// 3. On mismatch: Saves actual.png and diff.png to Artifacts/ for investigation
/// </summary>
public abstract class VisualSnapshotTestBase
{
    private static readonly string TestProjectRoot = GetTestProjectRoot();
    private static readonly string SnapshotsDir = Path.Combine(TestProjectRoot, "Snapshots");
    private static readonly string ArtifactsDir = Path.Combine(TestProjectRoot, "Artifacts");

    protected VisualSnapshotTestBase()
    {
        // Ensure directories exist
        Directory.CreateDirectory(SnapshotsDir);
        Directory.CreateDirectory(ArtifactsDir);
    }

    /// <summary>
    /// Finds the test project root by walking up from the bin directory.
    /// </summary>
    private static string GetTestProjectRoot()
    {
        var assemblyLocation = typeof(VisualSnapshotTestBase).Assembly.Location;
        var directory = Path.GetDirectoryName(assemblyLocation)!;

        // Walk up until we find the .csproj file
        while (directory != null)
        {
            if (Directory.GetFiles(directory, "*.csproj").Length > 0)
            {
                return directory;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new InvalidOperationException("Could not find test project root directory");
    }

    /// <summary>
    /// Assert that SVG content visually matches the golden snapshot.
    /// </summary>
    /// <param name="svgContent">The SVG XML content to render and compare</param>
    /// <param name="options">Options for comparison tolerance and behavior</param>
    /// <param name="testName">Auto-populated from calling method name</param>
    protected static void AssertMatchesSnapshot(
        string svgContent,
        SnapshotOptions? options = null,
        [CallerMemberName] string testName = "")
    {
        options ??= SnapshotOptions.Default;

        var goldenPngPath = GetSnapshotPath(testName, "png");
        var goldenSvgPath = GetSnapshotPath(testName, "svg");
        var actualPath = GetArtifactPath(testName, "actual");
        var diffPath = GetArtifactPath(testName, "diff");

        // Render SVG to bitmap
        using var actualBitmap = RenderSvgToBitmap(svgContent, options.Width, options.Height);

        if (!File.Exists(goldenPngPath))
        {
            // First run: save as golden and FAIL test for review
            SaveBitmap(actualBitmap, goldenPngPath);
            File.WriteAllText(goldenSvgPath, svgContent);
            Assert.Fail($"⚠️ NEW SNAPSHOT: Golden image and SVG created at '{goldenPngPath}' and '{goldenSvgPath}'.\n\nPlease:\n1. Review the image and SVG visually\n2. If correct, commit them to source control\n3. Re-run the test");
            return;
        }

        // Load golden image
        using var goldenBitmap = LoadBitmap(goldenPngPath);

        // Compare dimensions
        if (actualBitmap.Width != goldenBitmap.Width || actualBitmap.Height != goldenBitmap.Height)
        {
            SaveBitmap(actualBitmap, actualPath);
            Assert.Fail($"❌ DIMENSION MISMATCH:\nGolden: {goldenBitmap.Width}x{goldenBitmap.Height}\nActual: {actualBitmap.Width}x{actualBitmap.Height}\nSaved actual to: {actualPath}");
            return;
        }

        // Pixel-by-pixel comparison
        var comparison = CompareImages(goldenBitmap, actualBitmap, options);

        if (!comparison.IsMatch)
        {
            // Save actual and diff for investigation
            SaveBitmap(actualBitmap, actualPath);
            SaveBitmap(comparison.DiffBitmap!, diffPath);
            comparison.DiffBitmap?.Dispose();

            Assert.Fail(
                $"❌ SNAPSHOT MISMATCH:\n" +
                $"  Different pixels: {comparison.DifferentPixels:N0} / {comparison.TotalPixels:N0} ({comparison.DifferencePercentage:F2}%)\n" +
                $"  Max pixel delta: {comparison.MaxPixelDelta} (threshold: {options.MaxPixelDelta})\n" +
                $"  Threshold: {options.PixelDifferenceThreshold:F2}%\n\n" +
                $"Files saved:\n" +
                $"  Golden PNG:  {goldenPngPath}\n" +
                $"  Golden SVG:  {goldenSvgPath}\n" +
                $"  Actual:  {actualPath}\n" +
                $"  Diff:    {diffPath}\n\n" +
                $"To update: Delete '{goldenPngPath}' and '{goldenSvgPath}' and re-run test");
        }

        comparison.DiffBitmap?.Dispose();
    }

    /// <summary>
    /// Renders SVG content to a SkiaSharp bitmap at the specified dimensions.
    /// </summary>
    private static SKBitmap RenderSvgToBitmap(string svgContent, int width, int height)
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(svgContent));
        using var svg = new global::Svg.Skia.SKSvg();
        svg.Load(stream);

        // Create bitmap with white background
        var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        // Draw the SVG scaled to fit the bitmap dimensions
        if (svg.Picture != null)
        {
            var svgSize = svg.Picture.CullRect.Size;
            var scaleX = width / svgSize.Width;
            var scaleY = height / svgSize.Height;
            var scale = Math.Min(scaleX, scaleY);

            // Center the content
            var scaledWidth = svgSize.Width * scale;
            var scaledHeight = svgSize.Height * scale;
            var offsetX = (width - scaledWidth) / 2;
            var offsetY = (height - scaledHeight) / 2;

            canvas.Translate(offsetX, offsetY);
            canvas.Scale(scale);
            canvas.DrawPicture(svg.Picture);
        }

        return bitmap;
    }

    /// <summary>
    /// Compares two bitmaps pixel-by-pixel.
    /// </summary>
    private static ComparisonResult CompareImages(SKBitmap golden, SKBitmap actual, SnapshotOptions options)
    {
        var width = golden.Width;
        var height = golden.Height;

        var differentPixels = 0;
        var maxPixelDelta = 0;
        SKBitmap? diffBitmap = null;

        if (options.GenerateDiffImage)
        {
            diffBitmap = new SKBitmap(width, height);
        }

        var totalPixels = width * height;
        var bytesPerPixel = 4; // RGBA

        var goldenSpan = golden.GetPixelSpan();
        var actualSpan = actual.GetPixelSpan();
        var diffSpan = diffBitmap is null ? [] : diffBitmap.GetPixelSpan();

        for (int i = 0; i < totalPixels; i++)
        {
            var offset = i * bytesPerPixel;

            var deltaR = Math.Abs(goldenSpan[offset] - actualSpan[offset]);
            var deltaG = Math.Abs(goldenSpan[offset + 1] - actualSpan[offset + 1]);
            var deltaB = Math.Abs(goldenSpan[offset + 2] - actualSpan[offset + 2]);
            var deltaA = Math.Abs(goldenSpan[offset + 3] - actualSpan[offset + 3]);
            var delta = Math.Max(Math.Max(deltaR, deltaG), Math.Max(deltaB, deltaA));

            if (delta > options.MaxPixelDelta)
            {
                differentPixels++;
                maxPixelDelta = Math.Max(maxPixelDelta, delta);

                if (diffSpan.Length > 0)
                {
                    diffSpan[offset] = 0;     // B
                    diffSpan[offset + 1] = 0; // G
                    diffSpan[offset + 2] = 255; // R
                    diffSpan[offset + 3] = 255; // A
                }
            }
            else if (diffSpan.Length > 0)
            {
                var gray = (byte)((goldenSpan[offset] + goldenSpan[offset + 1] + goldenSpan[offset + 2]) / 3);
                diffSpan[offset] = gray;
                diffSpan[offset + 1] = gray;
                diffSpan[offset + 2] = gray;
                diffSpan[offset + 3] = 255;
            }
        }

        var differencePercentage = (differentPixels * 100.0) / totalPixels;
        var isMatch = differencePercentage <= options.PixelDifferenceThreshold &&
                      maxPixelDelta <= options.MaxPixelDelta;

        return new ComparisonResult
        {
            IsMatch = isMatch,
            DifferentPixels = differentPixels,
            TotalPixels = totalPixels,
            DifferencePercentage = differencePercentage,
            MaxPixelDelta = maxPixelDelta,
            DiffBitmap = diffBitmap
        };
    }

    private static void SaveBitmap(SKBitmap bitmap, string path)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(path);
        data.SaveTo(stream);
    }

    private static SKBitmap LoadBitmap(string path)
    {
        using var stream = File.OpenRead(path);
        return SKBitmap.Decode(stream);
    }

    private static string GetSnapshotPath(string testName, string extension) =>
        Path.Combine(SnapshotsDir, $"{testName}.{extension}");

    private static string GetArtifactPath(string testName, string suffix) =>
        Path.Combine(ArtifactsDir, $"{testName}_{suffix}.png");
}

/// <summary>
/// Result of image comparison.
/// </summary>
internal sealed class ComparisonResult
{
    public required bool IsMatch { get; init; }
    public required int DifferentPixels { get; init; }
    public required int TotalPixels { get; init; }
    public required double DifferencePercentage { get; init; }
    public required int MaxPixelDelta { get; init; }
    public SKBitmap? DiffBitmap { get; init; }
}
