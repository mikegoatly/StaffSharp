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
    /// Horizontal spacing between notes in staff spaces.
    /// </summary>
    public double NoteSpacing { get; set; } = 2;

    /// <summary>
    /// If set, the layout engine will stop processing after the specified pass. Can be used for 
    /// debugging layout issues.
    /// </summary>
    public string? BailAfterPass { get; set; }

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
