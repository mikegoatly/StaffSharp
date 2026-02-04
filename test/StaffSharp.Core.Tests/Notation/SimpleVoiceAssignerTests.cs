using StaffSharp;
using StaffSharp.Core.Notation;
using StaffSharp.Performance;
using StaffSharp.TestHelpers.Builders;

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
        var events = SymbolicNoteEventBuilder.Create()
            .WithDuration(1, 1)
            .AddNoteAt(Rational.Zero, MidiNote.C4)
            .AddNoteAt(Rational.Create(1, 1), MidiNote.D4)
            .AddNoteAt(Rational.Create(2, 1), MidiNote.E4)
            .Build();

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
        var events = SymbolicNoteEventBuilder.Create()
            .WithDuration(1, 1)
            .AddNoteAt(Rational.Zero, MidiNote.E4)
            .AddNoteAt(Rational.Create(1, 1), MidiNote.D4)
            .AddNoteAt(Rational.Create(2, 1), MidiNote.C4)
            .Build();

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
        var events = SymbolicNoteEventBuilder.Create()
            .AddNoteAt(Rational.Zero, MidiNote.C4, duration: Rational.Create(2, 1))
            .AddNoteAt(Rational.Create(1, 1), MidiNote.E4, duration: Rational.Create(2, 1))
            .Build();

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
        var events = SymbolicNoteEventBuilder.Create()
            .WithDuration(1, 1)
            .AddChord(MidiNote.G4, MidiNote.C4, MidiNote.E4)  // All at onset 0, overlapping
            .Build();

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
        var events = SymbolicNoteEventBuilder.Create()
            .WithDuration(4, 1)
            .AddNoteAt(Rational.Zero, MidiNote.C4)           // Beats 0-4
            .AddNoteAt(Rational.Create(1, 1), MidiNote.G4)   // Beats 1-5, overlaps with C4
            .AddNoteAt(Rational.Create(2, 1), MidiNote.E4)   // Beats 2-6, overlaps with C4 and G4
            .AddNoteAt(Rational.Create(3, 1), MidiNote.A4)   // Beats 3-7, overlaps with all previous
            .Build();

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
        var events = SymbolicNoteEventBuilder.Create()
            .WithVoiceHint(3)
            .AddNoteAt(Rational.Zero, MidiNote.C4, duration: Rational.Create(1, 1))
            .Build();

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
        var events = SymbolicNoteEventBuilder.Create()
            .WithDuration(1, 1)
            .AddNoteAt(Rational.Zero, MidiNote.C4)
            .AddNoteAt(Rational.Create(2, 1), MidiNote.C4)
            .Build();

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
        var events = SymbolicNoteEventBuilder.Create()
            .WithDuration(1, 1)
            .AddNoteAt(Rational.Zero, MidiNote.C4)
            .AddNoteAt(Rational.Create(1, 1), MidiNote.Create(60 + 11))  // 11 semitones up
            .Build();

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
        var events = SymbolicNoteEventBuilder.Create()
            .AddNoteAt(Rational.Zero, MidiNote.C4, duration: Rational.Create(2, 1))
            .AddNoteAt(Rational.Create(1, 1), MidiNote.Create(60 + 13), duration: Rational.Create(1, 1))  // 13 semitones up (beyond octave)
            .Build();

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
        var events = SymbolicNoteEventBuilder.Create()
            .WithDuration(1, 1)
            .AddChord(MidiNote.E4, MidiNote.G4, MidiNote.C4)
            .Build();

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
