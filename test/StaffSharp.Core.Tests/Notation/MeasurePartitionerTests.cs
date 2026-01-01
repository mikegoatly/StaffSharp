using StaffSharp;
using StaffSharp.Core.Notation;
using StaffSharp.Notation;
using StaffSharp.Performance;

namespace StaffSharp.Core.Tests.Notation;

/// <summary>
/// Tests for MeasurePartitioner class, focusing on measure boundary calculations.
/// </summary>
public sealed class MeasurePartitionerTests
{
    [Fact]
    public void PartitionIntoMeasures_SingleTimeSignature_CalculatesCorrectBoundaries()
    {
        // Arrange: 4/4 time throughout
        var tempoMap = new TempoMap(
            [new(Rational.Zero, 120.0)],
            [new(Rational.Zero, TimeSignature.CommonTime)]
        );

        var options = new NotationOptions();
        var partitioner = new MeasurePartitioner(tempoMap, options);

        // Create events in measures 1, 2, and 3
        var events = new List<VoiceAssignment>
        {
            new(new SymbolicNoteEvent(
                pitch: MidiNote.C4,
                onsetBeats: Rational.Zero,
                durationBeats: Rational.Create(4, 1),
                velocity: Velocity.MezzoForte), 1), // Measure 1

            new(new SymbolicNoteEvent(
                pitch: MidiNote.D4,
                onsetBeats: Rational.Create(4, 1),
                durationBeats: Rational.Create(4, 1),
                velocity: Velocity.MezzoForte), 1), // Measure 2

            new(new SymbolicNoteEvent(
                pitch: MidiNote.E4,
                onsetBeats: Rational.Create(8, 1),
                durationBeats: Rational.Create(4, 1),
                velocity: Velocity.MezzoForte), 1), // Measure 3
        };

        var voiceAssignments = new Dictionary<int, List<VoiceAssignment>>
        {
            { 1, events }
        };

        // Act
        var result = partitioner.PartitionIntoMeasures(voiceAssignments);

        // Assert
        Assert.Single(result);
        Assert.True(result.ContainsKey(1));

        var measures = result[1];
        Assert.Equal(3, measures.Count);
        Assert.Equal(1, measures[0].Number);
        Assert.Equal(2, measures[1].Number);
        Assert.Equal(3, measures[2].Number);
    }

    [Fact]
    public void PartitionIntoMeasures_TimeSignatureChange_HandlesCorrectly()
    {
        // Arrange: Start in 4/4, change to 3/4 at beat 8
        var tempoMap = new TempoMap(
            [new(Rational.Zero, 120.0)],
            [
                new(Rational.Zero, TimeSignature.CommonTime), // 4/4 at start
                new(Rational.Create(8, 1), new TimeSignature(3, 4)) // 3/4 at beat 8
            ]
        );

        var options = new NotationOptions();
        var partitioner = new MeasurePartitioner(tempoMap, options);

        // Create events: one in 4/4 section, one in 3/4 section
        var events = new List<VoiceAssignment>
        {
            new(new SymbolicNoteEvent(
                pitch: MidiNote.C4,
                onsetBeats: Rational.Zero,
                durationBeats: Rational.Create(4, 1),
                velocity: Velocity.MezzoForte), 1), // Measure 1 (4/4)

            new(new SymbolicNoteEvent(
                pitch: MidiNote.D4,
                onsetBeats: Rational.Create(8, 1),
                durationBeats: Rational.Create(3, 1),
                velocity: Velocity.MezzoForte), 1), // Measure 3 (first measure in 3/4)
        };

        var voiceAssignments = new Dictionary<int, List<VoiceAssignment>>
        {
            { 1, events }
        };

        // Act
        var result = partitioner.PartitionIntoMeasures(voiceAssignments);

        // Assert
        Assert.Single(result);
        var measures = result[1];

        // Should have measures 1 and 3 (measure 2 would be empty and might not be created)
        Assert.True(measures.Count >= 2);
        Assert.Equal(1, measures[0].Number);
    }

    [Fact]
    public void PartitionIntoMeasures_NoteSpanningMeasures_SplitsWithTies()
    {
        // Arrange: 4/4 time
        var tempoMap = new TempoMap(
            [new(Rational.Zero, 120.0)],
            [new(Rational.Zero, TimeSignature.CommonTime)]
        );

        var options = new NotationOptions();
        var partitioner = new MeasurePartitioner(tempoMap, options);

        // Create a note that spans from measure 1 into measure 2
        var events = new List<VoiceAssignment>
        {
            new(new SymbolicNoteEvent(
                pitch: MidiNote.C4,
                onsetBeats: Rational.Zero,
                durationBeats: Rational.Create(6, 1), // 6 beats = 1.5 measures
                velocity: Velocity.MezzoForte), 1),
        };

        var voiceAssignments = new Dictionary<int, List<VoiceAssignment>>
        {
            { 1, events }
        };

        // Act
        var result = partitioner.PartitionIntoMeasures(voiceAssignments);

        // Assert
        Assert.Single(result);
        var measures = result[1];

        Assert.Equal(2, measures.Count);

        // Measure 1 should have a note with TieType.Start
        var measure1Events = measures[0].Events;
        Assert.Single(measure1Events);
        var note1 = Assert.IsType<NotationNote>(measure1Events[0]);
        Assert.Equal(TieType.Start, note1.Tie);

        // Measure 2 should have a note with TieType.End
        var measure2Events = measures[1].Events;
        Assert.Single(measure2Events);
        var note2 = Assert.IsType<NotationNote>(measure2Events[0]);
        Assert.Equal(TieType.End, note2.Tie);
    }

    [Fact]
    public void PartitionIntoMeasures_GapBetweenNotes_InsertsRests()
    {
        // Arrange: 4/4 time
        var tempoMap = new TempoMap(
            [new(Rational.Zero, 120.0)],
            [new(Rational.Zero, TimeSignature.CommonTime)]
        );

        var options = new NotationOptions();
        var partitioner = new MeasurePartitioner(tempoMap, options);

        // Create notes with a gap
        var events = new List<VoiceAssignment>
        {
            new(new SymbolicNoteEvent(
                pitch: MidiNote.C4,
                onsetBeats: Rational.Zero,
                durationBeats: Rational.Create(2, 1), // 2 beats
                velocity: Velocity.MezzoForte), 1),

            // Gap from beat 2 to beat 3 (1 beat)

            new(new SymbolicNoteEvent(
                pitch: MidiNote.D4,
                onsetBeats: Rational.Create(3, 1), // Starts at beat 3
                durationBeats: Rational.Create(1, 1),
                velocity: Velocity.MezzoForte), 1),
        };

        var voiceAssignments = new Dictionary<int, List<VoiceAssignment>>
        {
            { 1, events }
        };

        // Act
        var result = partitioner.PartitionIntoMeasures(voiceAssignments);

        // Assert
        Assert.Single(result);
        var measures = result[1];

        Assert.Single(measures); // Both notes in measure 1
        var measure1Events = measures[0].Events;

        // Should have: note, rest, note
        Assert.Equal(3, measure1Events.Count);
        Assert.IsType<NotationNote>(measure1Events[0]);
        Assert.IsType<Rest>(measure1Events[1]);
        Assert.IsType<NotationNote>(measure1Events[2]);
    }

    [Fact]
    public void PartitionIntoMeasures_MultipleVoices_PartitionsIndependently()
    {
        // Arrange: 4/4 time
        var tempoMap = new TempoMap(
            [new(Rational.Zero, 120.0)],
            [new(Rational.Zero, TimeSignature.CommonTime)]
        );

        var options = new NotationOptions();
        var partitioner = new MeasurePartitioner(tempoMap, options);

        // Create events for two voices
        var voice1Events = new List<VoiceAssignment>
        {
            new(new SymbolicNoteEvent(
                pitch: MidiNote.C4,
                onsetBeats: Rational.Zero,
                durationBeats: Rational.Create(4, 1),
                velocity: Velocity.MezzoForte), 1),
        };

        var voice2Events = new List<VoiceAssignment>
        {
            new(new SymbolicNoteEvent(
                pitch: MidiNote.E4,
                onsetBeats: Rational.Create(4, 1),
                durationBeats: Rational.Create(4, 1),
                velocity: Velocity.MezzoForte), 2),
        };

        var voiceAssignments = new Dictionary<int, List<VoiceAssignment>>
        {
            { 1, voice1Events },
            { 2, voice2Events }
        };

        // Act
        var result = partitioner.PartitionIntoMeasures(voiceAssignments);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey(1));
        Assert.True(result.ContainsKey(2));
    }

    [Fact]
    public void PartitionIntoMeasures_EmptyVoiceAssignments_ReturnsEmpty()
    {
        // Arrange
        var tempoMap = new TempoMap(
            [new(Rational.Zero, 120.0)],
            [new(Rational.Zero, TimeSignature.CommonTime)]
        );

        var options = new NotationOptions();
        var partitioner = new MeasurePartitioner(tempoMap, options);

        var voiceAssignments = new Dictionary<int, List<VoiceAssignment>>();

        // Act
        var result = partitioner.PartitionIntoMeasures(voiceAssignments);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void PartitionIntoMeasures_ThreeFourTime_CalculatesCorrectBoundaries()
    {
        // Arrange: 3/4 time throughout
        var tempoMap = new TempoMap(
            [new(Rational.Zero, 120.0)],
            [new(Rational.Zero, new TimeSignature(3, 4))]
        );

        var options = new NotationOptions();
        var partitioner = new MeasurePartitioner(tempoMap, options);

        // Create events across 3 measures in 3/4 time
        var events = new List<VoiceAssignment>
        {
            new(new SymbolicNoteEvent(
                pitch: MidiNote.C4,
                onsetBeats: Rational.Zero,
                durationBeats: Rational.Create(3, 1),
                velocity: Velocity.MezzoForte), 1), // Measure 1 (beats 0-3)

            new(new SymbolicNoteEvent(
                pitch: MidiNote.D4,
                onsetBeats: Rational.Create(3, 1),
                durationBeats: Rational.Create(3, 1),
                velocity: Velocity.MezzoForte), 1), // Measure 2 (beats 3-6)

            new(new SymbolicNoteEvent(
                pitch: MidiNote.E4,
                onsetBeats: Rational.Create(6, 1),
                durationBeats: Rational.Create(3, 1),
                velocity: Velocity.MezzoForte), 1), // Measure 3 (beats 6-9)
        };

        var voiceAssignments = new Dictionary<int, List<VoiceAssignment>>
        {
            { 1, events }
        };

        // Act
        var result = partitioner.PartitionIntoMeasures(voiceAssignments);

        // Assert
        Assert.Single(result);
        var measures = result[1];

        Assert.Equal(3, measures.Count);
        Assert.Equal(1, measures[0].Number);
        Assert.Equal(2, measures[1].Number);
        Assert.Equal(3, measures[2].Number);
    }
}
