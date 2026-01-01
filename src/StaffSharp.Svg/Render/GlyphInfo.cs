namespace StaffSharp.Svg.Render;

/// <summary>
/// Represents a music notation glyph with its SVG path and bounding box information.
/// </summary>
/// <param name="Path">SVG path data for the glyph.</param>
/// <param name="MinX">Minimum X coordinate of the bounding box.</param>
/// <param name="MinY">Minimum Y coordinate of the bounding box.</param>
/// <param name="MaxX">Maximum X coordinate of the bounding box.</param>
/// <param name="MaxY">Maximum Y coordinate of the bounding box.</param>
/// <param name="AdvanceWidth">The advance width (horizontal spacing) for the glyph.</param>
public readonly record struct GlyphInfo(
    string Path,
    double MinX,
    double MinY,
    double MaxX,
    double MaxY,
    double AdvanceWidth)
{
    /// <summary>
    /// Gets the width of the glyph's bounding box.
    /// </summary>
    public double Width => MaxX - MinX;

    /// <summary>
    /// Gets the height of the glyph's bounding box.
    /// </summary>
    public double Height => MaxY - MinY;

    /// <summary>
    /// Calculates the scale factor needed to fit this glyph to a target height.
    /// </summary>
    /// <param name="targetHeight">The desired height in output units.</param>
    /// <returns>The scale factor to apply to the glyph.</returns>
    public double GetScaleForHeight(double targetHeight)
    {
        return Height > 0 ? targetHeight / Height : 1.0;
    }

    /// <summary>
    /// Calculates the scale factor needed to fit this glyph to a target width.
    /// </summary>
    /// <param name="targetWidth">The desired width in output units.</param>
    /// <returns>The scale factor to apply to the glyph.</returns>
    public double GetScaleForWidth(double targetWidth)
    {
        return Width > 0 ? targetWidth / Width : 1.0;
    }
}
