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
        new("renderDebugArtifacts", "Render debug artifacts (true/false)", "false"),
        new("foreground", "Foreground color (CSS format)", SvgRenderOptions.Default.Foreground),
        new("background", "Background color (CSS format)", SvgRenderOptions.Default.Background),
        new("bailAfterPass", "If set, the layout engine will stop processing after the specified pass")
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

        await ExportAsync(score, stream, ParseOptions(options), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Exports a notation score to SVG format.
    /// </summary>
    /// <param name="score">The score to export.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="options">Export options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task ExportAsync(
        NotationScore score,
        Stream stream,
        SvgRenderOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var renderedScore = Export(score, options);

        await stream.WriteAsync(Encoding.UTF8.GetBytes(renderedScore.ToString()), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Exports the specified musical score to a rendered SVG representation.
    /// </summary>
    /// <param name="score">The musical score to export. This parameter cannot be null.</param>
    /// <param name="options">Optional rendering options that control the appearance and behavior of the SVG output. If not specified, default
    /// options are used.</param>
    /// <returns>A RenderedScore object that can be dynamically highlighted and converted to SVG string.</returns>
    public static RenderedScore Export(NotationScore score, SvgRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(score);

        options ??= new();
        var context = options.ToSvgContext();

        // Layout first
        var layoutModel = LayoutEngine.Layout(score, context);

        // Render
        var svgRoot = SvgRenderer.Render(layoutModel, context);

        return new RenderedScore(svgRoot, layoutModel, score, context.Foreground);
    }

    private static SvgRenderOptions ParseOptions(IReadOnlyDictionary<string, string>? options)
    {
        var maxWidth = int.Parse(options?.GetValueOrDefault("maxWidth") ?? "1024", CultureInfo.InvariantCulture);
        var scale = double.Parse(options?.GetValueOrDefault("scale") ?? "1.0", CultureInfo.InvariantCulture);
        var marginsStr = options?.GetValueOrDefault("margins") ?? "40,40,40,40";
        var margins = marginsStr.Split(',').Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToArray();
        var staffSpace = int.Parse(options?.GetValueOrDefault("staffSpace") ?? "10", CultureInfo.InvariantCulture);
        var renderDebugArtifacts = bool.Parse(options?.GetValueOrDefault("renderDebugArtifacts") ?? "false");
        var background = options?.GetValueOrDefault("background") ?? SvgRenderOptions.Default.Background;
        var foreground = options?.GetValueOrDefault("foreground") ?? SvgRenderOptions.Default.Foreground;
        var bailAfterPass = options?.GetValueOrDefault("bailAfterPass");

        return new SvgRenderOptions
        {
            MaxWidth = maxWidth,
            Scale = scale,
            Margins = margins,
            StaffSpace = staffSpace,
            RenderDebugArtifacts = renderDebugArtifacts,
            Background = background,
            Foreground = foreground,
            BailAfterPass = bailAfterPass
        };

    }
}