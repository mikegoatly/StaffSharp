namespace StaffSharp;

using System.Globalization;
using System.Text;

using StaffSharp.Layout;
using StaffSharp.Notation;
using StaffSharp.Render;

/// <summary>
/// Exports a NotationScore to SVG format.
/// </summary>
public class SvgScoreExporter : IScoreExporter
{
    /// <inheritdoc />
    public IReadOnlyList<string> SupportedExtensions => [".svg"];

    /// <inheritdoc />
    public string FormatName => "SVG";

    /// <inheritdoc />
    public IReadOnlyList<ExportOption> AvailableOptions => [
        new("maxWidth", "Maximum width of a system in pixels before line break", "1024"),
        new("scale", "Global scale multiplier for staff-space units to pixels", "1.0"),
        new("margins", "Margins in pixels: left,right,top,bottom", "40,40,40,40"),
        new("staffSpace", "Pixels between staff lines", "10"),
        new("renderDebugArtifacts", "Render debug artifacts (true/false)", "false")
    ];

    /// <inheritdoc />
    public async Task ExportAsync(
        NotationScore score,
        Stream stream,
        IReadOnlyDictionary<string, string>? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(score);
        ArgumentNullException.ThrowIfNull(stream);

        var context = ParseOptions(options);
        var layoutModel = LayoutEngine.Layout(score, context);
        var svg = SvgRenderer.Render(layoutModel, context);

        await stream.WriteAsync(Encoding.UTF8.GetBytes(svg.ToString()), cancellationToken).ConfigureAwait(false);
    }

    private static SvgContext ParseOptions(IReadOnlyDictionary<string, string>? options)
    {
        var maxWidth = int.Parse(options?.GetValueOrDefault("maxWidth") ?? "1024", CultureInfo.InvariantCulture);
        var scale = double.Parse(options?.GetValueOrDefault("scale") ?? "1.0", CultureInfo.InvariantCulture);
        var marginsStr = options?.GetValueOrDefault("margins") ?? "40,40,40,40";
        var margins = marginsStr.Split(',').Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToArray();
        var staffSpace = int.Parse(options?.GetValueOrDefault("staffSpace") ?? "10", CultureInfo.InvariantCulture);
        var bailAfterPass = options?.GetValueOrDefault("bailAfterPass");
        var renderDebugArtifacts = bool.Parse(options?.GetValueOrDefault("renderDebugArtifacts") ?? "false");

        return new SvgContext
        {
            MaxWidth = maxWidth,
            Scale = scale,
            Margins = new Margins(margins[0], margins[1], margins[2], margins[3]),
            StaffSpace = staffSpace,
            BailAfterPass = bailAfterPass,
            RenderDebugArtifacts = renderDebugArtifacts,
            NoteHeadWholeWidth = CalculateNoteheadWidth(MusicGlyphs.NoteHeadWhole, staffSpace),
            NoteHeadHalfWidth = CalculateNoteheadWidth(MusicGlyphs.NoteHeadHalf, staffSpace),
            NoteHeadBlackWidth = CalculateNoteheadWidth(MusicGlyphs.NoteHeadBlack, staffSpace),
            HalfStaffSpace = staffSpace / 2D,
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