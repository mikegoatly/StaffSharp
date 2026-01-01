using StaffSharp.Notation;
using StaffSharp.Performance;

namespace StaffSharp.Core.Notation;

/// <summary>
/// Converts performance timeline data (IR1) to notation score (IR2).
/// </summary>
public sealed class NotationEngine : INotationEngine
{
    private readonly IVoiceAssigner _voiceAssigner;

    public NotationEngine(IVoiceAssigner? voiceAssigner = null)
    {
        _voiceAssigner = voiceAssigner ?? new SimpleVoiceAssigner();
    }

    /// <inheritdoc/>
    public NotationScore Convert(PerformanceTimeline timeline, NotationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(timeline);

        options ??= new NotationOptions();
        options.Validate();

        // Step 1: Assign events to voices
        var voiceAssignments = _voiceAssigner.AssignVoices(timeline.Events);

        // Step 2: Group assignments by voice
        var voiceGroups = voiceAssignments
            .GroupBy(a => a.VoiceNumber)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Step 3: Partition each voice into measures with ties
        var partitioner = new MeasurePartitioner(timeline.TempoMap, options);
        var voiceMeasures = partitioner.PartitionIntoMeasures(voiceGroups);

        // Step 4: Build the notation score structure
        var voices = new List<Voice>();
        foreach (var (voiceNumber, measures) in voiceMeasures.OrderBy(kvp => kvp.Key))
        {
            voices.Add(new Voice(voiceNumber, measures));
        }

        // Determine clef based on options and pitch range
        var clef = DetermineClef(timeline.Events, options);

        // Create a single part (monophonic or single instrument for now)
        // TODO: For wide pitch ranges (grand staff), split into treble and bass parts
        var part = new Part(
            name: timeline.Metadata.Title ?? "Part 1",
            clef: clef,
            voices: voices
        );

        // Create score metadata
        var metadata = new ScoreMetadata(
            Title: timeline.Metadata.Title,
            Composer: timeline.Metadata.Composer,
            KeySignature: options.DefaultKeySignature,
            TimeSignature: timeline.TempoMap.TimeSignatures.Count > 0 
                ? timeline.TempoMap.TimeSignatures[0].TimeSignature 
                : TimeSignature.CommonTime,
            Tempo: (int)Math.Round(timeline.TempoMap.TempoChanges.Count > 0 
                ? timeline.TempoMap.TempoChanges[0].BeatsPerMinute 
                : 120.0)
        );

        return new NotationScore(metadata, new[] { part });
    }

    /// <summary>
    /// Determines the appropriate clef based on options and pitch range analysis.
    /// </summary>
    private static Clef DetermineClef(IReadOnlyList<IPerformanceEvent> events, NotationOptions options)
    {
        // If user forced a specific clef, use it
        if (options.ClefPreference != ClefPreference.Auto)
        {
            return options.ClefPreference switch
            {
                ClefPreference.ForceTreble => Clef.Treble,
                ClefPreference.ForceBass => Clef.Bass,
                ClefPreference.ForceAlto => Clef.Alto,
                ClefPreference.ForceTenor => Clef.Tenor,
                _ => Clef.Treble
            };
        }

        // Auto-detect based on pitch range
        // Extract all pitches from events
        var pitches = new List<int>();
        foreach (var ev in events)
        {
            var pitch = ev switch
            {
                QuantizedNoteEvent qne => qne.RawEvent.Pitch.MidiNumber,
                SymbolicNoteEvent sne => sne.Pitch.MidiNumber,
                _ => -1
            };

            if (pitch >= 0)
            {
                pitches.Add(pitch);
            }
        }

        if (pitches.Count == 0)
        {
            // No pitched events, default to treble
            return Clef.Treble;
        }

        // Calculate average pitch
        var averagePitch = pitches.Average();

        // Middle C is MIDI 60
        // Treble clef center is around B4 (MIDI 71)
        // Bass clef center is around D3 (MIDI 50)
        // Use MIDI 60 (Middle C) as the pivot point

        if (averagePitch >= 60)
        {
            // Average pitch is at or above middle C - use treble clef
            return Clef.Treble;
        }
        else
        {
            // Average pitch is below middle C - use bass clef
            return Clef.Bass;
        }
    }
}
