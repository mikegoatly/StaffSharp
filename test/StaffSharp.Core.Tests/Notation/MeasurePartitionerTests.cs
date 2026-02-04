using StaffSharp;
using StaffSharp.Core.Notation;
using StaffSharp.Notation;
using StaffSharp.Performance;
using StaffSharp.TestHelpers;
using StaffSharp.TestHelpers.Builders;

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
        var partitioner = new MeasurePartitioner(tempoMap);

        var voiceAssignments = SymbolicNoteEventBuilder.Create()
            .WithDuration(4, 1)
            .AddNoteAt(Rational.Zero, MidiNote.C4)     // Measure 1
            .AddNoteAt(Rational.Create(4, 1), MidiNote.D4)  // Measure 2
            .AddNoteAt(Rational.Create(8, 1), MidiNote.E4)  // Measure 3
            .Build()
            .AssignToVoice(1)
            .ToVoiceDictionary();

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
        var partitioner = new MeasurePartitioner(tempoMap);

        var voiceAssignments = SymbolicNoteEventBuilder.Create()
            .AddNoteAt(Rational.Zero, MidiNote.C4, duration: Rational.Create(4, 1))  // Measure 1 (4/4)
            .AddNoteAt(Rational.Create(8, 1), MidiNote.D4, duration: Rational.Create(3, 1))  // Measure 3 (3/4)
            .Build()
            .AssignToVoice(1)
            .ToVoiceDictionary();

        // Act
        var result = partitioner.PartitionIntoMeasures(voiceAssignments);

        // Assert
        Assert.Single(result);
        var measures = result[1];
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
        var partitioner = new MeasurePartitioner(tempoMap);

        var voiceAssignments = SymbolicNoteEventBuilder.Create()
            .AddNoteAt(Rational.Zero, MidiNote.C4, duration: Rational.Create(6, 1))  // 6 beats = 1.5 measures
            .Build()
            .AssignToVoice(1)
            .ToVoiceDictionary();

        // Act
        var result = partitioner.PartitionIntoMeasures(voiceAssignments);

        // Assert
        var measures = result[1];
        Assert.Equal(2, measures.Count);

        measures[0].AssertSequence()
            .Note(PitchClass.C, SymbolicDuration.Whole, tie: TieMarkerType.Start)
            .AndNoMore();

        measures[1].AssertSequence()
            .Note(PitchClass.C, SymbolicDuration.Half, tie: TieMarkerType.Stop)
            .AndNoMore();
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
        var partitioner = new MeasurePartitioner(tempoMap);

        var voiceAssignments = SymbolicNoteEventBuilder.Create()
            .AddNoteAt(Rational.Zero, MidiNote.C4, duration: Rational.Create(2, 1))  // 2 beats
            .AddNoteAt(Rational.Create(3, 1), MidiNote.D4, duration: Rational.Create(1, 1))  // Gap from beat 2 to 3
            .Build()
            .AssignToVoice(1)
            .ToVoiceDictionary();

        // Act
        var result = partitioner.PartitionIntoMeasures(voiceAssignments);

        // Assert
        Assert.Single(result);
        var measures = result[1];
        Assert.Single(measures);

        result[1].AssertSequence()
            .Note(PitchClass.C, SymbolicDuration.Half)
            .Rest(SymbolicDuration.Quarter)
            .Note(PitchClass.D, SymbolicDuration.Quarter)
            .AndNoMore();
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
        var partitioner = new MeasurePartitioner(tempoMap);

        var voice1 = SymbolicNoteEventBuilder.Create()
            .WithDuration(4, 1)
            .AddNoteAt(Rational.Zero, MidiNote.C4)
            .Build()
            .AssignToVoice(1);

        var voiceAssignments = SymbolicNoteEventBuilder.Create()
            .WithDuration(4, 1)
            .AddNoteAt(Rational.Create(4, 1), MidiNote.E4)
            .Build()
            .AssignToVoice(2)
            .ToVoiceDictionary(otherAssignments: voice1);

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
        var partitioner = new MeasurePartitioner(tempoMap);

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
        var partitioner = new MeasurePartitioner(tempoMap);

        var voiceAssignments = SymbolicNoteEventBuilder.Create()
            .WithDuration(3, 1)
            .AddNoteAt(Rational.Zero, MidiNote.C4)                  // Measure 1
            .AddNoteAt(Rational.Create(3, 1), MidiNote.D4)          // Measure 2
            .AddNoteAt(Rational.Create(6, 1), MidiNote.E4)          // Measure 3
            .Build()
            .AssignToVoice(1)
            .ToVoiceDictionary();

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

    [Fact]
    public void PartitionIntoMeasures_SimultaneousNotesInSameVoice_CreatesChord()
    {
        // Arrange: 4/4 time
        var tempoMap = new TempoMap(
            [new(Rational.Zero, 120.0)],
            [new(Rational.Zero, TimeSignature.CommonTime)]
        );

        var options = new NotationOptions();
        var partitioner = new MeasurePartitioner(tempoMap);

        var voiceAssignments = SymbolicNoteEventBuilder.Create()
            .WithDuration(2, 1)
            .AddChord(MidiNote.C4, MidiNote.E4, MidiNote.G4)
            .Build()
            .AssignToVoice(1)
            .ToVoiceDictionary();

        // Act
        var result = partitioner.PartitionIntoMeasures(voiceAssignments);

        // Assert
        result[1].AssertSequence()
            .Chord([PitchClass.C, PitchClass.E, PitchClass.G], SymbolicDuration.Half)
            .AndNoMore();
    }

    [Fact]
    public void PartitionIntoMeasures_OverlappingNotesStaggeredStarts_CreatesTemporalSegments()
    {
        // Arrange: 4/4 time, two notes in same voice with staggered starts
        // C4: beats 0-4 (4 beats)
        // E4: beats 2-4 (2 beats, starts while C4 is still playing)
        // Expected: C4 split into two segments: solo (0-2) and chord (2-4)
        var tempoMap = new TempoMap(
            [new(Rational.Zero, 120.0)],
            [new(Rational.Zero, TimeSignature.CommonTime)]
        );

        var partitioner = new MeasurePartitioner(tempoMap);

        var voiceAssignments = SymbolicNoteEventBuilder.Create()
            .AddNoteAt(Rational.Zero, MidiNote.C4, duration: Rational.Create(4, 1))  // C4
            .AddNoteAt(Rational.Create(2, 1), MidiNote.E4, duration: Rational.Create(2, 1))  // E4
            .Build()
            .AssignToVoice(1)
            .ToVoiceDictionary();

        // Act
        var result = partitioner.PartitionIntoMeasures(voiceAssignments);

        // Assert
        Assert.Single(result);
        var measures = result[1];
        Assert.Single(measures);

        var measure = measures[0];
        Assert.Equal(2, measure.Events.Count);

        measure.AssertSequence()
            .Note(PitchClass.C, SymbolicDuration.Half) // Solo C4
            .Chord([PitchClass.C, PitchClass.E], SymbolicDuration.Half) // C4+E4 chord
            .AndNoMore();
    }

    [Fact]
    public void PartitionIntoMeasures_SimultaneousStartDifferentDurations_TreatsAsPolyphonic()
    {
        // Arrange: 4/4 time
        // Simulates left-hand chord (4 beats) + right-hand melody (1 beat) starting together
        var tempoMap = new TempoMap(
            [new(Rational.Zero, 120.0)],
            [new(Rational.Zero, TimeSignature.CommonTime)]
        );

        var partitioner = new MeasurePartitioner(tempoMap);

        var voiceAssignments = SymbolicNoteEventBuilder.Create()
            .AddNoteAt(Rational.Zero, MidiNote.C2, duration: Rational.Create(4, 1))  // Left hand bass
            .AddNoteAt(Rational.Zero, MidiNote.E2, duration: Rational.Create(4, 1))  // Left hand
            .AddNoteAt(Rational.Zero, MidiNote.G2, duration: Rational.Create(4, 1))  // Left hand
            .AddNoteAt(Rational.Zero, MidiNote.C5, duration: Rational.Create(1, 1))  // Right hand melody
            .Build()
            .AssignToVoice(1)
            .ToVoiceDictionary();

        // Act
        var result = partitioner.PartitionIntoMeasures(voiceAssignments);

        // Assert
        // Should create temporal segments representing the full duration:
        // 1. All 4 notes together for 1 beat (until shortest note ends)
        // 2. Remaining 3 notes continue for 3 more beats
        // This preserves the left-hand chord's full 4-beat duration, avoiding the problem
        // where the entire chord would be truncated to 1 beat if treated as a simple chord.
        Assert.Single(result);
        var measures = result[1];
        Assert.Single(measures);

        var measure = measures[0];

        measure.AssertSequence()
            // First segment: all 4 notes sound together for 1 beat
            .Chord([PitchClass.C, PitchClass.E, PitchClass.G, PitchClass.C], SymbolicDuration.Quarter)
            // Second segment: left-hand notes continue for remaining 3 beats
            .Chord([PitchClass.C, PitchClass.E, PitchClass.G], new SymbolicDuration(NoteDurationBase.Half, dots: 1))
            .AndNoMore();
    }
}
