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

        // Step 3: Check if grand staff is needed
        var useGrandStaff = StaffSplitter.ShouldUseGrandStaff(timeline.Events, options);

        Part part;
        if (useGrandStaff)
        {
            // Create grand staff (treble + bass)
            part = CreateGrandStaffPart(timeline, voiceAssignments, options);
        }
        else
        {
            // Create single-staff part
            part = CreateSingleStaffPart(timeline, voiceAssignments, options);
        }

        // Build TieSpans and SlurSpans from markers on notes
        SpanBuilder.BuildTieSpans(part);
        SpanBuilder.BuildSlurSpans(part);

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

        return new NotationScore(metadata, [part]);
    }

    /// <summary>
    /// Creates a single-staff part from voice assignments.
    /// </summary>
    private static Part CreateSingleStaffPart(
        PerformanceTimeline timeline,
        IReadOnlyList<VoiceAssignment> voiceAssignments,
        NotationOptions options)
    {
        // Group measures by voice
        var voiceGroups = voiceAssignments
            .GroupBy(a => a.VoiceNumber)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Partition into measures
        var partitioner = new MeasurePartitioner(timeline.TempoMap, options);
        var voiceMeasures = partitioner.PartitionIntoMeasures(voiceGroups);

        // Build voices
        var voices = new List<Voice>();
        foreach (var (voiceNumber, measures) in voiceMeasures.OrderBy(kvp => kvp.Key))
        {
            voices.Add(new Voice(voiceNumber, measures));
        }

        // Determine clef
        var clef = DetermineClef(timeline.Events, options);

        // Create single-staff part using legacy constructor
        return new Part(
            name: timeline.Metadata.Title ?? "Part 1",
            clef: clef,
            voices: voices
        );
    }

    /// <summary>
    /// Creates a grand staff part (treble + bass) from voice assignments.
    /// </summary>
    private static Part CreateGrandStaffPart(
        PerformanceTimeline timeline,
        IReadOnlyList<VoiceAssignment> voiceAssignments,
        NotationOptions options)
    {
        // Split into treble and bass based on pitch
        var (trebleAssignments, bassAssignments) = StaffSplitter.SplitVoiceAssignments(
            voiceAssignments,
            options.GrandStaffSplitPoint
        );

        // Renumber voices within each staff
        trebleAssignments = StaffSplitter.RenumberVoices(trebleAssignments);
        bassAssignments = StaffSplitter.RenumberVoices(bassAssignments);

        // Create partitioner
        var partitioner = new MeasurePartitioner(timeline.TempoMap, options);

        // Create treble staff (Staff 1)
        var trebleStaff = CreateStaffFromAssignments(
            trebleAssignments,
            staffNumber: 1,
            clef: Clef.Treble,
            partitioner
        );

        // Create bass staff (Staff 2)
        var bassStaff = CreateStaffFromAssignments(
            bassAssignments,
            staffNumber: 2,
            clef: Clef.Bass,
            partitioner
        );

        // Create grand staff part
        return new Part(
            name: timeline.Metadata.Title ?? "Piano",
            staves: [trebleStaff, bassStaff]
        );
    }

    /// <summary>
    /// Creates a staff from voice assignments.
    /// </summary>
    private static Staff CreateStaffFromAssignments(
        List<VoiceAssignment> assignments,
        int staffNumber,
        Clef clef,
        MeasurePartitioner partitioner)
    {
        if (assignments.Count == 0)
        {
            // Empty staff - create single empty voice
            return new Staff(
                number: staffNumber,
                clef: clef,
                voices: [new Voice(1, [])]
            );
        }

        // Group by voice
        var voiceGroups = assignments
            .GroupBy(a => a.VoiceNumber)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Partition into measures
        var voiceMeasures = partitioner.PartitionIntoMeasures(voiceGroups);

        // Build voices
        var voices = voiceMeasures
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => new Voice(kvp.Key, kvp.Value))
            .ToList();

        return new Staff(
            number: staffNumber,
            clef: clef,
            voices: voices
        );
    }

    /// <summary>
    /// Determines the appropriate clef based on options and pitch range analysis.
    /// </summary>
    private static Clef DetermineClef(IReadOnlyList<IPerformanceEvent> events, NotationOptions options)
    {
        // If user forced a specific clef, use it (but not AutoGrandStaff - that's handled separately)
        if (options.ClefPreference != ClefPreference.Auto && options.ClefPreference != ClefPreference.AutoGrandStaff)
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
        var pitches = events
            .Select(e => e.Pitch.MidiNumber)
            .ToList();

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
