using StaffSharp;
using StaffSharp.Core.Notation;
using StaffSharp.Performance;

namespace StaffSharp.Core.Tests.Notation;

/// <summary>
/// Tests for SimpleVoiceAssigner class, focusing on correct voice number assignment.
/// </summary>
public sealed class SimpleVoiceAssignerTests
{
    [Fact]
    public void AssignVoices_AscendingPitches_ReuseSameVoice()
    {
        // Arrange: Sequential notes with ascending pitches should stay in voice 1
        var assigner = new SimpleVoiceAssigner();
        var events = new List<IPerformanceEvent>
        {
            new SymbolicNoteEvent(MidiNote.C4, Rational.Zero, Rational.Create(1, 1), Velocity.MezzoForte),
            new SymbolicNoteEvent(MidiNote.D4, Rational.Create(1, 1), Rational.Create(1, 1), Velocity.MezzoForte),
            new SymbolicNoteEvent(MidiNote.E4, Rational.Create(2, 1), Rational.Create(1, 1), Velocity.MezzoForte),
        };

        // Act
        var result = assigner.AssignVoices(events);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(result, assignment => Assert.Equal(1, assignment.VoiceNumber));
    }

    [Fact]
    public void AssignVoices_DescendingPitches_ReuseSameVoice()
    {
        // Arrange: Sequential notes with descending pitches should stay in voice 1
        var assigner = new SimpleVoiceAssigner();
        var events = new List<IPerformanceEvent>
        {
            new SymbolicNoteEvent(MidiNote.E4, Rational.Zero, Rational.Create(1, 1), Velocity.MezzoForte),
            new SymbolicNoteEvent(MidiNote.D4, Rational.Create(1, 1), Rational.Create(1, 1), Velocity.MezzoForte),
            new SymbolicNoteEvent(MidiNote.C4, Rational.Create(2, 1), Rational.Create(1, 1), Velocity.MezzoForte),
        };

        // Act
        var result = assigner.AssignVoices(events);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(result, assignment => Assert.Equal(1, assignment.VoiceNumber));
    }

    [Fact]
    public void AssignVoices_OverlappingNotesDifferentPitches_CreatesSeparateVoices()
    {
        // Arrange: Two notes that overlap in time with different pitches
        var assigner = new SimpleVoiceAssigner();
        var events = new List<IPerformanceEvent>
        {
            new SymbolicNoteEvent(MidiNote.C4, Rational.Zero, Rational.Create(2, 1), Velocity.MezzoForte),
            new SymbolicNoteEvent(MidiNote.E4, Rational.Create(1, 1), Rational.Create(2, 1), Velocity.MezzoForte),
        };

        // Act
        var result = assigner.AssignVoices(events);

        // Assert
        Assert.Equal(2, result.Count);
        // C4 starts in voice 1, but when E4 (higher pitch) overlaps,
        // E4 becomes voice 1 and C4 is renumbered to voice 2
        Assert.Equal(2, result[0].VoiceNumber); // C4 renumbered to voice 2
        Assert.Equal(1, result[1].VoiceNumber); // E4 (higher) gets voice 1
    }

    [Fact]
    public void AssignVoices_InterleavedPitches_AssignsCorrectVoiceNumbers()
    {
        // Arrange: High, low, middle pattern - tests voice insertion
        var assigner = new SimpleVoiceAssigner();
        var events = new List<IPerformanceEvent>
        {
            new SymbolicNoteEvent(MidiNote.G4, Rational.Zero, Rational.Create(1, 1), Velocity.MezzoForte), // High
            new SymbolicNoteEvent(MidiNote.C4, Rational.Zero, Rational.Create(1, 1), Velocity.MezzoForte), // Low (overlaps)
            new SymbolicNoteEvent(MidiNote.E4, Rational.Zero, Rational.Create(1, 1), Velocity.MezzoForte), // Middle (overlaps)
        };

        // Act
        var result = assigner.AssignVoices(events);

        // Assert
        Assert.Equal(3, result.Count);

        // Higher pitches should get lower voice numbers
        var g4Assignment = result.First(a => ((SymbolicNoteEvent)a.Event).Pitch == MidiNote.G4);
        var e4Assignment = result.First(a => ((SymbolicNoteEvent)a.Event).Pitch == MidiNote.E4);
        var c4Assignment = result.First(a => ((SymbolicNoteEvent)a.Event).Pitch == MidiNote.C4);

        // Processing order: G4 (voice 1), then C4 overlaps (voice 2), then E4 overlaps (inserts as voice 2, others shift)
        // Final: G4=voice 1, E4=voice 2, C4=voice 3
        Assert.Equal(1, g4Assignment.VoiceNumber); // Highest pitch = voice 1
        Assert.InRange(e4Assignment.VoiceNumber, 1, 3); // Middle pitch
        Assert.InRange(c4Assignment.VoiceNumber, 1, 3); // Lowest pitch
        // Ensure correct ordering
        Assert.True(g4Assignment.VoiceNumber < e4Assignment.VoiceNumber);
        Assert.True(e4Assignment.VoiceNumber < c4Assignment.VoiceNumber);
    }

    [Fact]
    public void AssignVoices_NoDuplicateVoiceNumbers_WhenOverlapping()
    {
        // Arrange: Pattern with actual overlapping notes
        var assigner = new SimpleVoiceAssigner();
        var events = new List<IPerformanceEvent>
        {
            new SymbolicNoteEvent(MidiNote.C4, Rational.Zero, Rational.Create(4, 1), Velocity.MezzoForte), // Beats 0-4
            new SymbolicNoteEvent(MidiNote.G4, Rational.Create(1, 1), Rational.Create(4, 1), Velocity.MezzoForte), // Beats 1-5, overlaps with C4
            new SymbolicNoteEvent(MidiNote.E4, Rational.Create(2, 1), Rational.Create(4, 1), Velocity.MezzoForte), // Beats 2-6, overlaps with C4 and G4
            new SymbolicNoteEvent(MidiNote.A4, Rational.Create(3, 1), Rational.Create(4, 1), Velocity.MezzoForte), // Beats 3-7, overlaps with all previous
        };

        // Act
        var result = assigner.AssignVoices(events);

        // Assert - all 4 events overlap, so need 4 distinct voices
        Assert.Equal(4, result.Count);

        var voiceNumbers = result.Select(a => a.VoiceNumber).ToList();

        // The key requirement: no duplicate voice numbers when notes overlap
        Assert.Equal(4, voiceNumbers.Distinct().Count());
    }

    [Fact]
    public void AssignVoices_VoiceHintProvided_UsesHint()
    {
        // Arrange: Event with explicit voice hint
        var assigner = new SimpleVoiceAssigner();
        var events = new List<IPerformanceEvent>
        {
            new SymbolicNoteEvent(MidiNote.C4, Rational.Zero, Rational.Create(1, 1), Velocity.MezzoForte, voiceHint: 3),
        };

        // Act
        var result = assigner.AssignVoices(events);

        // Assert
        Assert.Single(result);
        Assert.Equal(3, result[0].VoiceNumber);
    }

    [Fact]
    public void AssignVoices_EmptyEventList_ReturnsEmpty()
    {
        // Arrange
        var assigner = new SimpleVoiceAssigner();
        var events = new List<IPerformanceEvent>();

        // Act
        var result = assigner.AssignVoices(events);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void AssignVoices_NonOverlappingNotesSamePitch_ReuseVoice()
    {
        // Arrange: Same pitch, no overlap - should reuse voice
        var assigner = new SimpleVoiceAssigner();
        var events = new List<IPerformanceEvent>
        {
            new SymbolicNoteEvent(MidiNote.C4, Rational.Zero, Rational.Create(1, 1), Velocity.MezzoForte),
            new SymbolicNoteEvent(MidiNote.C4, Rational.Create(2, 1), Rational.Create(1, 1), Velocity.MezzoForte),
        };

        // Act
        var result = assigner.AssignVoices(events);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].VoiceNumber);
        Assert.Equal(1, result[1].VoiceNumber); // Can reuse voice since no overlap
    }

    [Fact]
    public void AssignVoices_PitchWithinOctave_ReusesVoice()
    {
        // Arrange: Notes within an octave should reuse voice
        var assigner = new SimpleVoiceAssigner();
        var events = new List<IPerformanceEvent>
        {
            new SymbolicNoteEvent(MidiNote.C4, Rational.Zero, Rational.Create(1, 1), Velocity.MezzoForte),
            new SymbolicNoteEvent(MidiNote.Create(60 + 11), Rational.Create(1, 1), Rational.Create(1, 1), Velocity.MezzoForte), // 11 semitones up
        };

        // Act
        var result = assigner.AssignVoices(events);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].VoiceNumber);
        Assert.Equal(1, result[1].VoiceNumber); // Within octave, reuses voice
    }

    [Fact]
    public void AssignVoices_PitchBeyondOctave_CreatesNewVoice()
    {
        // Arrange: Notes more than an octave apart should create new voice when overlapping
        var assigner = new SimpleVoiceAssigner();
        var events = new List<IPerformanceEvent>
        {
            new SymbolicNoteEvent(MidiNote.C4, Rational.Zero, Rational.Create(2, 1), Velocity.MezzoForte),
            new SymbolicNoteEvent(MidiNote.Create(60 + 13), Rational.Create(1, 1), Rational.Create(1, 1), Velocity.MezzoForte), // 13 semitones up (beyond octave)
        };

        // Act
        var result = assigner.AssignVoices(events);

        // Assert
        Assert.Equal(2, result.Count);
        // C4 starts in voice 1, but higher note (even beyond octave) that overlaps
        // gets inserted as voice 1, renumbering C4 to voice 2
        Assert.Equal(2, result[0].VoiceNumber); // C4 renumbered to voice 2
        Assert.Equal(1, result[1].VoiceNumber); // Higher note gets voice 1
    }

    [Fact]
    public void AssignVoices_ThreeVoiceChord_AssignsHighToLowVoiceNumbers()
    {
        // Arrange: Three simultaneous notes at different pitches
        var assigner = new SimpleVoiceAssigner();
        var events = new List<IPerformanceEvent>
        {
            new SymbolicNoteEvent(MidiNote.E4, Rational.Zero, Rational.Create(1, 1), Velocity.MezzoForte),
            new SymbolicNoteEvent(MidiNote.G4, Rational.Zero, Rational.Create(1, 1), Velocity.MezzoForte),
            new SymbolicNoteEvent(MidiNote.C4, Rational.Zero, Rational.Create(1, 1), Velocity.MezzoForte),
        };

        // Act
        var result = assigner.AssignVoices(events);

        // Assert
        Assert.Equal(3, result.Count);

        var g4 = result.First(a => ((SymbolicNoteEvent)a.Event).Pitch == MidiNote.G4);
        var e4 = result.First(a => ((SymbolicNoteEvent)a.Event).Pitch == MidiNote.E4);
        var c4 = result.First(a => ((SymbolicNoteEvent)a.Event).Pitch == MidiNote.C4);

        // Higher pitches get lower voice numbers (soprano, alto, bass convention)
        Assert.True(g4.VoiceNumber < e4.VoiceNumber);
        Assert.True(e4.VoiceNumber < c4.VoiceNumber);
    }
}
