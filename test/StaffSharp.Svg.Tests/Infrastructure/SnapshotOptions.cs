namespace StaffSharp.Svg.Tests.Infrastructure;

/// <summary>
/// Options for snapshot comparison behavior.
/// </summary>
/// <param name="Width"> Width of rendered image in pixels. </param>
/// <param name="Height"> Height of rendered image in pixels. </param>
/// <param name="PixelDifferenceThreshold"> Maximum percentage of pixels that can differ (0-100). </param>
/// <param name="MaxPixelDelta"> Maximum delta per color channel (0-255) to consider pixels equal. </param>
/// <param name="GenerateDiffImage"> Whether to generate a diff image showing differences. </param>
public record SnapshotOptions(int Width, int Height, double PixelDifferenceThreshold, int MaxPixelDelta, bool GenerateDiffImage)
{
    /// <summary>
    /// Default options: 800x600, 0.5% pixel difference threshold, max delta of 5 per channel.
    /// </summary>
    public static readonly SnapshotOptions Default = new(
        Width: 800,
        Height: 600,
        PixelDifferenceThreshold: 0.5, // 0.5% of pixels can differ
        MaxPixelDelta: 5, // Max difference per color channel
        GenerateDiffImage: true);
}
