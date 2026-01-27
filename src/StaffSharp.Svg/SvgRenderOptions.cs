using StaffSharp.Render;

namespace StaffSharp;

/// <summary>
/// Color configuration for SVG rendering.
/// </summary>
public record SvgRenderOptions
{
    /// <summary>
    /// Foreground color for musical notation elements (default: black).
    /// </summary>
    public string Foreground { get; init; } = "black";

    /// <summary>
    /// Background color for the SVG canvas (default: white).
    /// </summary>
    public string Background { get; init; } = "white";

    /// <summary>
    /// Default color configuration (black notation on white background).
    /// </summary>
    public static SvgRenderOptions Default => new();

    /// <summary>
    /// Gets the maximum allowable width for the element, in pixels.
    /// </summary>
    public int MaxWidth { get; init; }

    /// <summary>
    /// Gets the scale factor to apply to the rendered SVG.
    /// </summary>
    public double Scale { get; init; }

    /// <summary>
    /// Gets the margins applied to the element as a read-only list of integers.
    /// </summary>
    public IReadOnlyList<int> Margins { get; init; } = [40, 40, 40, 40];

    /// <summary>
    /// Gets the number of pixels per staff space (distance between staff lines).
    /// </summary>
    public int StaffSpace { get; init; }

    /// <summary>
    /// Gets a value indicating whether debug artifacts are rendered during SVG output.
    /// </summary>
    public bool RenderDebugArtifacts { get; init; }

    /// <summary>
    /// If set, the layout engine will stop processing after the specified pass. Can be used for 
    /// debugging layout issues.
    /// </summary>
    public string? BailAfterPass { get; init; }

    internal SvgContext ToSvgContext()
    {
        return new SvgContext
        {
            MaxWidth = MaxWidth,
            Scale = Scale,
            Margins = new Margins(Margins[0], Margins[1], Margins[2], Margins[3]),
            StaffSpace = StaffSpace,
            RenderDebugArtifacts = RenderDebugArtifacts,
            Foreground = Foreground,
            Background = Background,
            NoteHeadWholeWidth = CalculateNoteheadWidth(MusicGlyphs.NoteHeadWhole, StaffSpace),
            NoteHeadHalfWidth = CalculateNoteheadWidth(MusicGlyphs.NoteHeadHalf, StaffSpace),
            NoteHeadBlackWidth = CalculateNoteheadWidth(MusicGlyphs.NoteHeadBlack, StaffSpace),
            HalfStaffSpace = StaffSpace / 2D,
            BailAfterPass = BailAfterPass,
        };
    }

    private static double CalculateNoteheadWidth(GlyphInfo glyph, int staffSpace)
    {
        // Noteheads are scaled to 1.0 staff space in height
        var targetHeight = 1.0 * staffSpace;
        var scale = glyph.Height > 0 ? targetHeight / glyph.Height : 1.0;

        // Round scale to 2dp
        // This ensures stem positions align with the actual rendered notehead width
        var roundedScale = Math.Round(scale, 2);
        return glyph.Width * roundedScale;
    }
}
