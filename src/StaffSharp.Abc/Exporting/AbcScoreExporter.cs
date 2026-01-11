namespace StaffSharp.Abc.Exporting;

using StaffSharp;
using StaffSharp.Notation;

/// <summary>
/// Score exporter for ABC notation format.
/// </summary>
public sealed class AbcScoreExporter : IScoreExporter
{
    public IReadOnlyList<string> SupportedExtensions { get; } = [".abc"];

    public string FormatName => "ABC Notation";

    public IReadOnlyList<ExportOption> AvailableOptions { get; } =
    [
        new ExportOption(
            "defaultNoteLength",
            "Default note length (L: header). Format: \"1/8\", \"1/4\", \"1/16\". (ABC only)",
            "1/8")
    ];

    public async Task ExportAsync(
        NotationScore score,
        Stream stream,
        IReadOnlyDictionary<string, string>? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(score);
        ArgumentNullException.ThrowIfNull(stream);

        // Parse options
        var exportOptions = new AbcExportOptions();

        if (options != null && options.TryGetValue("defaultNoteLength", out var defaultNoteLengthValue))
        {
            // Parse format like "1/8" or "1/4"
            var parts = defaultNoteLengthValue.Split('/');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out var numerator) &&
                int.TryParse(parts[1], out var denominator))
            {
                exportOptions = exportOptions with
                {
                    DefaultNoteLength = Rational.Create(numerator, denominator)
                };
            }
        }

        await AbcExporter.ExportAsync(score, stream, exportOptions, cancellationToken).ConfigureAwait(false);
    }
}
