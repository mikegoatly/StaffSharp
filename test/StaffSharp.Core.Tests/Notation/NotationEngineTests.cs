using StaffSharp;
using StaffSharp.Core.Notation;
using StaffSharp.Notation;
using StaffSharp.Performance;
using StaffSharp.TestHelpers;
using StaffSharp.TestHelpers.Builders;

namespace StaffSharp.Core.Tests.Notation;

/// <summary>
/// Tests for NotationEngine class, focusing on clef detection and IR1 to IR2 conversion.
/// </summary>
public sealed class NotationEngineTests
{
    [Fact]
    public void Convert_HighPitchRange_AutoDetectsTrebleClef()
    {
        // Arrange: Notes in the treble range (C5-G5, MIDI 72-79)
        var events = SymbolicNoteEventBuilder.Create()
            .AddNoteAt(Rational.Zero, MidiNote.C5)                   // C5 at beat 0
            .AddNoteAt(Rational.Create(1, 4), MidiNote.E5)           // E5 at beat 0.25
            .AddNoteAt(Rational.Create(1, 2), MidiNote.G5)           // G5 at beat 0.5
            .Build();

        var tempoMap = new TempoMap(
            [new(Rational.Zero, 120.0)],
            [new(Rational.Zero, TimeSignature.CommonTime)]
        );

        var timeline = new PerformanceTimeline(
            events: events,
            tempoMap: tempoMap,
            metadata: new PerformanceMetadata(Title: "High Notes Test")
        );

        var engine = new NotationEngine();
        var options = new NotationOptions(); // Default is ClefPreference.Auto

        // Act
        var score = engine.Convert(timeline, options);

        // Assert
        Assert.Single(score.Parts);
        Assert.Equal(Clef.Treble, score.Parts[0].Clef);
    }

    [Fact]
    public void Convert_LowPitchRange_AutoDetectsBassClef()
    {
        // Arrange: Notes in the bass range (C3-G3, MIDI 48-55)
        var events = SymbolicNoteEventBuilder.Create()
            .AddNoteAt(Rational.Zero, MidiNote.C3)                   // C3
            .AddNoteAt(Rational.Create(1, 4), MidiNote.E3)           // E3
            .AddNoteAt(Rational.Create(1, 2), MidiNote.G3)           // G3
            .Build();

        var tempoMap = new TempoMap(
            [new(Rational.Zero, 120.0)],
            [new(Rational.Zero, TimeSignature.CommonTime)]
        );

        var timeline = new PerformanceTimeline(
            events: events,
            tempoMap: tempoMap,
            metadata: new PerformanceMetadata(Title: "Low Notes Test")
        );

        var engine = new NotationEngine();
        var options = new NotationOptions(); // Default is ClefPreference.Auto

        // Act
        var score = engine.Convert(timeline, options);

        // Assert
        Assert.Single(score.Parts);
        Assert.Equal(Clef.Bass, score.Parts[0].Clef);
    }

    [Fact]
    public void Convert_ForceTrebleClef_UsesTrebleRegardlessOfPitchRange()
    {
        // Arrange: Low notes that would normally use bass clef
        var events = SymbolicNoteEventBuilder.Create()
            .AddNoteAt(Rational.Zero, MidiNote.E2)                   // E2
            .AddNoteAt(Rational.Create(1, 4), MidiNote.G2)           // G2
            .Build();

        var tempoMap = new TempoMap(
            [new(Rational.Zero, 120.0)],
            [new(Rational.Zero, TimeSignature.CommonTime)]
        );

        var timeline = new PerformanceTimeline(
            events: events,
            tempoMap: tempoMap,
            metadata: new PerformanceMetadata(Title: "Force Treble Test")
        );

        var engine = new NotationEngine();
        var options = new NotationOptions { ClefPreference = ClefPreference.ForceTreble };

        // Act
        var score = engine.Convert(timeline, options);

        // Assert
        Assert.Single(score.Parts);
        Assert.Equal(Clef.Treble, score.Parts[0].Clef);
    }

    [Fact]
    public void Convert_ForceBassClef_UsesBassRegardlessOfPitchRange()
    {
        // Arrange: High notes that would normally use treble clef
        var events = SymbolicNoteEventBuilder.Create()
            .AddNoteAt(Rational.Zero, MidiNote.C5)                   // C5
            .AddNoteAt(Rational.Create(1, 4), MidiNote.E5)           // E5
            .Build();

        var tempoMap = new TempoMap(
            [new(Rational.Zero, 120.0)],
            [new(Rational.Zero, TimeSignature.CommonTime)]
        );

        var timeline = new PerformanceTimeline(
            events: events,
            tempoMap: tempoMap,
            metadata: new PerformanceMetadata(Title: "Force Bass Test")
        );

        var engine = new NotationEngine();
        var options = new NotationOptions { ClefPreference = ClefPreference.ForceBass };

        // Act
        var score = engine.Convert(timeline, options);

        // Assert
        Assert.Single(score.Parts);
        Assert.Equal(Clef.Bass, score.Parts[0].Clef);
    }

    [Fact]
    public void Convert_ForceAltoClef_UsesAltoClef()
    {
        // Arrange
        var events = SymbolicNoteEventBuilder.SingleNote(MidiNote.C4);

        var tempoMap = new TempoMap(
            [new(Rational.Zero, 120.0)],
            [new(Rational.Zero, TimeSignature.CommonTime)]
        );

        var timeline = new PerformanceTimeline(
            events: events,
            tempoMap: tempoMap,
            metadata: new PerformanceMetadata(Title: "Alto Clef Test")
        );

        var engine = new NotationEngine();
        var options = new NotationOptions { ClefPreference = ClefPreference.ForceAlto };

        // Act
        var score = engine.Convert(timeline, options);

        // Assert
        Assert.Single(score.Parts);
        Assert.Equal(Clef.Alto, score.Parts[0].Clef);
    }

    [Fact]
    public void Convert_ForceTenorClef_UsesTenorClef()
    {
        // Arrange
        var events = SymbolicNoteEventBuilder.SingleNote(MidiNote.C4);

        var tempoMap = new TempoMap(
            [new(Rational.Zero, 120.0)],
            [new(Rational.Zero, TimeSignature.CommonTime)]
        );

        var timeline = new PerformanceTimeline(
            events: events,
            tempoMap: tempoMap,
            metadata: new PerformanceMetadata(Title: "Tenor Clef Test")
        );

        var engine = new NotationEngine();
        var options = new NotationOptions { ClefPreference = ClefPreference.ForceTenor };

        // Act
        var score = engine.Convert(timeline, options);

        // Assert
        Assert.Single(score.Parts);
        Assert.Equal(Clef.Tenor, score.Parts[0].Clef);
    }

    [Fact]
    public void Convert_MiddleC_UsesTrebleClef()
    {
        // Arrange: Middle C (MIDI 60) exactly - should default to treble (>= 60)
        var events = SymbolicNoteEventBuilder.SingleNote(MidiNote.C4); // C4 = MIDI 60

        var tempoMap = new TempoMap(
            [new(Rational.Zero, 120.0)],
            [new(Rational.Zero, TimeSignature.CommonTime)]
        );

        var timeline = new PerformanceTimeline(
            events: events,
            tempoMap: tempoMap,
            metadata: new PerformanceMetadata(Title: "Middle C Test")
        );

        var engine = new NotationEngine();
        var options = new NotationOptions(); // Auto detection

        // Act
        var score = engine.Convert(timeline, options);

        // Assert
        Assert.Single(score.Parts);
        Assert.Equal(Clef.Treble, score.Parts[0].Clef);
    }

    [Fact]
    public void Convert_EmptyTimeline_DefaultsToTrebleClef()
    {
        // Arrange: No events
        var events = new List<IPerformanceEvent>();

        var tempoMap = new TempoMap(
            [new(Rational.Zero, 120.0)],
            [new(Rational.Zero, TimeSignature.CommonTime)]
        );

        var timeline = new PerformanceTimeline(
            events: events,
            tempoMap: tempoMap,
            metadata: new PerformanceMetadata(Title: "Empty Test")
        );

        var engine = new NotationEngine();
        var options = new NotationOptions(); // Auto detection

        // Act
        var score = engine.Convert(timeline, options);

        // Assert
        Assert.Single(score.Parts);
        Assert.Equal(Clef.Treble, score.Parts[0].Clef); // Default when no pitched events
    }

    [Fact]
    public void Convert_WidePitchRange_AutoCreatesGrandStaff()
    {
        // Arrange: Notes spanning from C2 (MIDI 36) to C6 (MIDI 84) = 48 semitones (> 24 threshold)
        var events = SymbolicNoteEventBuilder.Create()
            // Bass range notes
            .AddNoteAt(Rational.Zero, MidiNote.C2)                   // C2
            .AddNoteAt(Rational.Create(1, 4), MidiNote.C3)           // C3
            .AddNoteAt(Rational.Create(1, 2), MidiNote.E3)           // E3
            // Treble range notes
            .AddNoteAt(Rational.Create(3, 4), MidiNote.C5)           // C5
            .AddNoteAt(Rational.Create(4, 4), MidiNote.E5)           // E5
            .AddNoteAt(Rational.Create(5, 4), MidiNote.C6)           // C6
            .Build();

        var tempoMap = new TempoMap(
            [new(Rational.Zero, 120.0)],
            [new(Rational.Zero, TimeSignature.CommonTime)]
        );

        var timeline = new PerformanceTimeline(
            events: events,
            tempoMap: tempoMap,
            metadata: new PerformanceMetadata(Title: "Wide Range Piano Test")
        );

        var engine = new NotationEngine();
        var options = new NotationOptions { ClefPreference = ClefPreference.AutoGrandStaff };

        // Act
        var score = engine.Convert(timeline, options);

        // Assert
        score.AssertGrandStaff()
            .IsGrandStaff()
            .HasStandardClefs()
            .HasNotesSplitAt(60,  // Default split point
                (36, "bass"),     // C2
                (48, "bass"),     // C3
                (52, "bass"),     // E3
                (72, "treble"),   // C5
                (76, "treble"),   // E5
                (84, "treble")    // C6
            );
    }

    [Fact]
    public void Convert_NarrowPitchRange_UsesSingleStaff()
    {
        // Arrange: Notes within 1 octave (12 semitones < 24 threshold)
        var events = SymbolicNoteEventBuilder.Create()
            .AddNoteAt(Rational.Zero, MidiNote.C4)                   // C4 (MIDI 60)
            .AddNoteAt(Rational.Create(1, 4), MidiNote.E4)           // E4
            .AddNoteAt(Rational.Create(1, 2), MidiNote.G4)           // G4
            .AddNoteAt(Rational.Create(3, 4), MidiNote.C5)           // C5
            .Build();

        var tempoMap = new TempoMap(
            [new(Rational.Zero, 120.0)],
            [new(Rational.Zero, TimeSignature.CommonTime)]
        );

        var timeline = new PerformanceTimeline(
            events: events,
            tempoMap: tempoMap,
            metadata: new PerformanceMetadata(Title: "Narrow Range Test")
        );

        var engine = new NotationEngine();
        var options = new NotationOptions { ClefPreference = ClefPreference.AutoGrandStaff };

        // Act
        var score = engine.Convert(timeline, options);

        // Assert
        score.AssertGrandStaff()
            .IsNotGrandStaff();
    }

    [Fact]
    public void Convert_CustomSplitPoint_SplitsCorrectly()
    {
        // Arrange: Use split point at E4 (MIDI 64) instead of C4 (MIDI 60)
        // Range: 36 to 84 = 48 semitones (> 24 threshold)
        var events = SymbolicNoteEventBuilder.Create()
            .AddNoteAt(Rational.Zero, MidiNote.C2)                   // C2 - should go to bass
            .AddNoteAt(Rational.Create(1, 4), MidiNote.C4)           // C4 (MIDI 60) - should go to bass
            .AddNoteAt(Rational.Create(1, 2), MidiNote.DSharp4)      // D#4 - should go to bass
            .AddNoteAt(Rational.Create(3, 4), MidiNote.E4)           // E4 - should go to treble
            .AddNoteAt(Rational.Create(4, 4), MidiNote.C5)           // C5 - should go to treble
            .AddNoteAt(Rational.Create(5, 4), MidiNote.C6)           // C6 - should go to treble
            .Build();

        var tempoMap = new TempoMap(
            [new(Rational.Zero, 120.0)],
            [new(Rational.Zero, TimeSignature.CommonTime)]
        );

        var timeline = new PerformanceTimeline(
            events: events,
            tempoMap: tempoMap,
            metadata: new PerformanceMetadata(Title: "Custom Split Point Test")
        );

        var engine = new NotationEngine();
        var options = new NotationOptions
        {
            ClefPreference = ClefPreference.AutoGrandStaff,
            GrandStaffSplitPoint = 64  // E4 instead of C4
        };

        // Act
        var score = engine.Convert(timeline, options);

        // Assert
        score.AssertGrandStaff()
            .IsGrandStaff()
            .HasStandardClefs()
            .HasNotesSplitAt(64,
                (36, "bass"),   // C2
                (60, "bass"),   // C4
                (63, "bass"),   // D#4
                (64, "treble"), // E4
                (72, "treble"), // C5
                (84, "treble")  // C6
            );
    }

    [Fact]
    public void Convert_AutoGrandStaffWithExactlyThreshold_UsesSingleStaff()
    {
        // Arrange: Exactly 24 semitones range (should not trigger grand staff - needs to exceed threshold)
        var events = SymbolicNoteEventBuilder.Create()
            .AddNoteAt(Rational.Zero, MidiNote.C4)                   // C4 (MIDI 60)
            .AddNoteAt(Rational.Create(1, 4), MidiNote.C6)           // C6 (MIDI 84) - exactly 24 semitones
            .Build();

        var tempoMap = new TempoMap(
            [new(Rational.Zero, 120.0)],
            [new(Rational.Zero, TimeSignature.CommonTime)]
        );

        var timeline = new PerformanceTimeline(
            events: events,
            tempoMap: tempoMap,
            metadata: new PerformanceMetadata(Title: "Exact Threshold Test")
        );

        var engine = new NotationEngine();
        var options = new NotationOptions { ClefPreference = ClefPreference.AutoGrandStaff };

        // Act
        var score = engine.Convert(timeline, options);

        // Assert
        score.AssertGrandStaff()
            .IsNotGrandStaff();
    }

    [Fact]
    public void Convert_AutoGrandStaffWithEmptyStaff_CreatesEmptyVoice()
    {
        // Arrange: All notes go to treble (all >= 60), bass staff should be empty
        var events = SymbolicNoteEventBuilder.Create()
            .AddNoteAt(Rational.Zero, MidiNote.C4)                   // C4 (MIDI 60)
            .AddNoteAt(Rational.Create(1, 4), MidiNote.C6)           // C6 (MIDI 84)
            .AddNoteAt(Rational.Create(1, 2), MidiNote.CSharp6)      // C#6 (MIDI 85) - exceeds threshold
            .Build();

        var tempoMap = new TempoMap(
            [new(Rational.Zero, 120.0)],
            [new(Rational.Zero, TimeSignature.CommonTime)]
        );

        var timeline = new PerformanceTimeline(
            events: events,
            tempoMap: tempoMap,
            metadata: new PerformanceMetadata(Title: "Empty Bass Staff Test")
        );

        var engine = new NotationEngine();
        var options = new NotationOptions { ClefPreference = ClefPreference.AutoGrandStaff };

        // Act
        var score = engine.Convert(timeline, options);

        // Assert
        score.AssertGrandStaff()
            .IsGrandStaff()
            .HasEmptyBassStaff();
    }

    [Fact]
    public void Convert_NotesCrossingBarLines_CreatesTieSpans()
    {
        // Arrange: Create a note that crosses a bar line
        // In 4/4 time (4 beats per measure):
        // - Note starts at beat 3.5
        // - Note duration is 2 beats
        // - Note ends at beat 5.5 (next measure, beat 1.5)
        // This should create two notes with a TieSpan between them
        var events = SymbolicNoteEventBuilder.Create()
            .AddNoteAt(Rational.Create(7, 2), MidiNote.C4, duration: Rational.Create(2, 1))  // 3.5 beats, 2 beats duration
            .Build();

        var tempoMap = new TempoMap(
            [new(Rational.Zero, 120.0)],
            [new(Rational.Zero, TimeSignature.CommonTime)]  // 4/4 time
        );

        var timeline = new PerformanceTimeline(
            events: events,
            tempoMap: tempoMap,
            metadata: new PerformanceMetadata(Title: "Tie Test")
        );

        var engine = new NotationEngine();
        var options = new NotationOptions();

        // Act
        var score = engine.Convert(timeline, options);

        // Assert
        var part = score.Parts[0];

        // Should have ties
        Assert.NotEmpty(part.Ties);
        Assert.Single(part.Ties);

        // Verify the tie connects two notes
        var tie = part.Ties[0];
        Assert.NotNull(tie.StartEvent);
        Assert.NotNull(tie.EndEvent);
        Assert.NotEqual(tie.StartEvent, tie.EndEvent);

        // Both notes should be C4
        Assert.IsType<NotationNote>(tie.StartEvent);
        Assert.IsType<NotationNote>(tie.EndEvent);
        var startNote = (NotationNote)tie.StartEvent;
        var endNote = (NotationNote)tie.EndEvent;
        Assert.Equal(PitchClass.C, startNote.Pitch.PitchClass);
        Assert.Equal(4, startNote.Pitch.Octave);
        Assert.Equal(PitchClass.C, endNote.Pitch.PitchClass);
        Assert.Equal(4, endNote.Pitch.Octave);
    }

    [Fact]
    public void Convert_NoteSpanningMultipleMeasures_CreatesMultipleTieSpans()
    {
        // Arrange: Create a long note spanning 3 measures
        // In 4/4 time (4 beats per measure):
        // - Note starts at beat 0
        // - Note duration is 10 beats
        // - Should create 3 tied notes across 3 measures (4 + 4 + 2 beats)
        var events = SymbolicNoteEventBuilder.Create()
            .AddNoteAt(Rational.Zero, MidiNote.G5, duration: Rational.Create(10, 1))  // 10 beats duration
            .Build();

        var tempoMap = new TempoMap(
            [new(Rational.Zero, 120.0)],
            [new(Rational.Zero, TimeSignature.CommonTime)]  // 4/4 time
        );

        var timeline = new PerformanceTimeline(
            events: events,
            tempoMap: tempoMap,
            metadata: new PerformanceMetadata(Title: "Long Tie Test")
        );

        var engine = new NotationEngine();
        var options = new NotationOptions();

        // Act
        var score = engine.Convert(timeline, options);

        // Assert
        var part = score.Parts[0];

        // Should have 2 ties connecting 3 notes (note1->note2, note2->note3)
        Assert.Equal(2, part.Ties.Count);

        // Verify first tie
        var tie1 = part.Ties[0];
        Assert.IsType<NotationNote>(tie1.StartEvent);
        Assert.IsType<NotationNote>(tie1.EndEvent);
        var note1 = (NotationNote)tie1.StartEvent;
        var note2 = (NotationNote)tie1.EndEvent;
        Assert.Equal(PitchClass.G, note1.Pitch.PitchClass);
        Assert.Equal(5, note1.Pitch.Octave);
        Assert.Equal(PitchClass.G, note2.Pitch.PitchClass);
        Assert.Equal(5, note2.Pitch.Octave);

        // Verify second tie
        var tie2 = part.Ties[1];
        Assert.Equal(tie1.EndEvent, tie2.StartEvent);  // Second note is shared
        Assert.IsType<NotationNote>(tie2.EndEvent);
        var note3 = (NotationNote)tie2.EndEvent;
        Assert.Equal(PitchClass.G, note3.Pitch.PitchClass);
        Assert.Equal(5, note3.Pitch.Octave);
    }
}
