namespace StaffSharp.Abc.Exporting;

using System.Text;

using StaffSharp.Notation;

/// <summary>
/// Core logic for exporting a NotationScore to ABC notation.
/// </summary>
internal static class AbcExporter
{
    public static async Task ExportAsync(
        NotationScore score,
        Stream stream,
        AbcExportOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(score);
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(options);

        var sb = new StringBuilder();

        // Write headers (X:, T:, C:, M:, L:, Q:, K:)
        AbcHeaderWriter.WriteHeaders(sb, score.Metadata, options);

        // Write note content
        // For now, assume single part, single staff, possibly multiple voices
        if (score.Parts.Count > 0)
        {
            var part = score.Parts[0];

            // Get all voices from all staves
            var voices = part.Voices;

            if (voices.Count == 1)
            {
                // Single voice - write measures directly
                var voice = voices[0];
                var markerMap = BuildMarkerMap(voice, part);
                AbcEventWriter.WriteMeasures(sb, voice.Measures, markerMap, options);
            }
            else if (voices.Count > 1)
            {
                // Multiple voices - write V: directives
                foreach (var voice in voices)
                {
                    sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"V:{voice.Number}");
                    sb.AppendLine();
                    var markerMap = BuildMarkerMap(voice, part);
                    AbcEventWriter.WriteMeasures(sb, voice.Measures, markerMap, options);
                }
            }
        }

        // Write to stream
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Builds a map of markers (ties, slurs) for each event in a voice.
    /// This reverses the import process: TieSpans/SlurSpans → markers on events.
    /// </summary>
    private static Dictionary<INotationEvent, EventMarkers> BuildMarkerMap(
        Voice voice,
        Part part)
    {
        var markers = new Dictionary<INotationEvent, EventMarkers>();

        // Initialize all events with empty markers
        foreach (var measure in voice.Measures)
        {
            foreach (var evt in measure.Events)
            {
                markers[evt] = new EventMarkers();
            }
        }

        // Apply TieSpans: mark start and end events
        foreach (var tieSpan in part.Ties)
        {
            // Only process ties for this voice
            if (tieSpan.StartVoiceNumber != voice.Number)
            {
                continue;
            }

            if (markers.TryGetValue(tieSpan.StartEvent, out var startMarkers))
            {
                startMarkers.HasTie = true;
            }
        }

        // Apply SlurSpans: mark start and end events
        foreach (var slurSpan in part.Slurs)
        {
            // Only process slurs for this voice
            if (slurSpan.StartVoiceNumber != voice.Number)
            {
                continue;
            }

            if (markers.TryGetValue(slurSpan.StartEvent, out var startMarkers))
            {
                startMarkers.SlurStarts.Add(slurSpan);
            }

            if (markers.TryGetValue(slurSpan.EndEvent, out var endMarkers))
            {
                endMarkers.SlurEnds.Add(slurSpan);
            }
        }

        return markers;
    }
}

/// <summary>
/// Tracks tie and slur markers for a single notation event.
/// </summary>
internal sealed class EventMarkers
{
    public bool HasTie { get; set; }
    public List<SlurSpan> SlurStarts { get; } = [];
    public List<SlurSpan> SlurEnds { get; } = [];
}
