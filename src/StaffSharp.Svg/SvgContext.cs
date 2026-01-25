using StaffSharp.Notation;
using StaffSharp.Render;

namespace StaffSharp;

/// <summary>
/// Context for SVG export operations.
/// </summary>
internal record SvgContext
{
    /// <summary>
    /// Tracks glyphs used during rendering for deduplication.
    /// Maps glyph ID to GlyphInfo for later definition emission.
    /// </summary>
    private readonly Dictionary<string, GlyphInfo> _usedGlyphs = [];

    public int MaxWidth { get; set; }
    public double Scale { get; set; }
    public Margins Margins { get; set; }
    public int StaffSpace { get; set; }

    /// <summary>
    /// Width of a whole notehead in pixels, calculated from glyph dimensions.
    /// </summary>
    internal double NoteHeadWholeWidth { get; set; }

    /// <summary>
    /// Width of a half notehead in pixels, calculated from glyph dimensions.
    /// </summary>
    internal double NoteHeadHalfWidth { get; set; }

    /// <summary>
    /// Width of a black (quarter/eighth) notehead in pixels, calculated from glyph dimensions.
    /// </summary>
    internal double NoteHeadBlackWidth { get; set; }

    /// <summary>
    /// Gets or sets the amount of space taken by <see cref="StaffSpace"/> divided by 2.
    /// </summary>
    internal double HalfStaffSpace { get; set; }

    /// <summary>
    /// Horizontal spacing between notes in staff spaces.
    /// </summary>
    public double NoteSpacing { get; set; } = 2;

    /// <summary>
    /// If set, the layout engine will stop processing after the specified pass. Can be used for 
    /// debugging layout issues.
    /// </summary>
    public string? BailAfterPass { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether debug artifacts are rendered during output generation.
    /// </summary>
    /// <remarks>Enable this property to display additional visual elements or overlays that assist in
    /// debugging or development. These artifacts are not intended for production use and may affect performance or
    /// output appearance.</remarks>
    public bool RenderDebugArtifacts { get; set; }

    /// <summary>
    /// Registers a glyph as used during rendering.
    /// </summary>
    /// <param name="glyph">The glyph to register.</param>
    internal void RegisterGlyph(GlyphInfo glyph)
    {
        _usedGlyphs.TryAdd(glyph.Id, glyph);
    }

    /// <summary>
    /// Gets all glyphs that were registered during rendering.
    /// </summary>
    internal IReadOnlyCollection<GlyphInfo> UsedGlyphs => _usedGlyphs.Values;

    /// <summary>
    /// Gets the foreground color used to render notation elements.
    /// </summary>
    public string Foreground { get; init; } = "black";

    /// <summary>
    /// Gets the background color for the score.
    /// </summary>
    public string Background { get; init; } = "white";

    internal double GetNoteheadWidth(NoteDurationBase duration)
    {
        return duration switch
        {
            NoteDurationBase.Whole => NoteHeadWholeWidth,
            NoteDurationBase.Half => NoteHeadHalfWidth,
            _ => NoteHeadBlackWidth
        };
    }
}
