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

        // Create a single part (monophonic or single instrument for now)
        var part = new Part(
            name: timeline.Metadata.Title ?? "Part 1",
            clef: Clef.Treble, // TODO: Determine from pitch range
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
}
