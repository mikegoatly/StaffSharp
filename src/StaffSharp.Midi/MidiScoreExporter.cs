namespace StaffSharp.Midi;

using StaffSharp;
using StaffSharp.Notation;

/// <summary>
/// Score exporter for MIDI format.
/// </summary>
public sealed class MidiScoreExporter : IScoreExporter
{
    public IReadOnlyList<string> SupportedExtensions { get; } = [".mid", ".midi"];

    public string FormatName => "MIDI";

    public IReadOnlyList<ExportOption> AvailableOptions { get; } =
    [
        new ExportOption(
            "tpqn",
            "Ticks per quarter note (MIDI time division). Higher values = more precise timing. Common values: 96, 192, 384, 480, 960. (MIDI only)",
            "480")
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
        var exportOptions = new MidiExportOptions();

        if (options != null && options.TryGetValue("tpqn", out var tpqnValue))
        {
            if (int.TryParse(tpqnValue, out var tpqn))
            {
                exportOptions = exportOptions with { TicksPerQuarterNote = tpqn };
            }
        }

        await MidiExporter.ExportAsync(score, stream, exportOptions, cancellationToken).ConfigureAwait(false);
    }
}
