using StaffSharp.Render;

namespace StaffSharp;

/// <summary>
/// Context for SVG export operations.
/// </summary>
public record SvgContext
{
    public int MaxWidth { get; set; }
    public double Scale { get; set; }
    public Margins Margins { get; set; }
    public int StaffSpace { get; set; }

    /// <summary>
    /// Horizontal spacing between notes in staff spaces.
    /// </summary>
    public double NoteSpacing { get; set; } = 2;
    public string? BailAfterPass { get; set; }

    /// <summary>
    /// Tracks glyphs used during rendering for deduplication.
    /// Maps glyph ID to GlyphInfo for later definition emission.
    /// </summary>
    private readonly Dictionary<string, GlyphInfo> _usedGlyphs = [];

    /// <summary>
    /// Registers a glyph as used during rendering.
    /// </summary>
    /// <param name="glyph">The glyph to register.</param>
    public void RegisterGlyph(GlyphInfo glyph)
    {
        _usedGlyphs.TryAdd(glyph.Id, glyph);
    }

    /// <summary>
    /// Gets all glyphs that were registered during rendering.
    /// </summary>
    public IReadOnlyCollection<GlyphInfo> UsedGlyphs => _usedGlyphs.Values;
}
