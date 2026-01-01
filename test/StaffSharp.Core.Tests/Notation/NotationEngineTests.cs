using StaffSharp;
using StaffSharp.Core.Notation;
using StaffSharp.Notation;
using StaffSharp.Performance;

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
        var events = new List<IPerformanceEvent>
        {
            new SymbolicNoteEvent(MidiNote.Create(72), Rational.Zero, Rational.Create(1, 4), Velocity.MezzoForte),      // C5
            new SymbolicNoteEvent(MidiNote.Create(76), Rational.Create(1, 4), Rational.Create(1, 4), Velocity.MezzoForte), // E5
            new SymbolicNoteEvent(MidiNote.Create(79), Rational.Create(1, 2), Rational.Create(1, 4), Velocity.MezzoForte), // G5
        };

        var tempoMap = new TempoMap(
            new List<TempoChange> { new(Rational.Zero, 120.0) },
            new List<TimeSignatureChange> { new(Rational.Zero, TimeSignature.CommonTime) }
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
        var events = new List<IPerformanceEvent>
        {
            new SymbolicNoteEvent(MidiNote.Create(48), Rational.Zero, Rational.Create(1, 4), Velocity.MezzoForte),      // C3
            new SymbolicNoteEvent(MidiNote.Create(52), Rational.Create(1, 4), Rational.Create(1, 4), Velocity.MezzoForte), // E3
            new SymbolicNoteEvent(MidiNote.Create(55), Rational.Create(1, 2), Rational.Create(1, 4), Velocity.MezzoForte), // G3
        };

        var tempoMap = new TempoMap(
            new List<TempoChange> { new(Rational.Zero, 120.0) },
            new List<TimeSignatureChange> { new(Rational.Zero, TimeSignature.CommonTime) }
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
        var events = new List<IPerformanceEvent>
        {
            new SymbolicNoteEvent(MidiNote.Create(40), Rational.Zero, Rational.Create(1, 4), Velocity.MezzoForte),      // E2
            new SymbolicNoteEvent(MidiNote.Create(43), Rational.Create(1, 4), Rational.Create(1, 4), Velocity.MezzoForte), // G2
        };

        var tempoMap = new TempoMap(
            new List<TempoChange> { new(Rational.Zero, 120.0) },
            new List<TimeSignatureChange> { new(Rational.Zero, TimeSignature.CommonTime) }
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
        var events = new List<IPerformanceEvent>
        {
            new SymbolicNoteEvent(MidiNote.Create(72), Rational.Zero, Rational.Create(1, 4), Velocity.MezzoForte),      // C5
            new SymbolicNoteEvent(MidiNote.Create(76), Rational.Create(1, 4), Rational.Create(1, 4), Velocity.MezzoForte), // E5
        };

        var tempoMap = new TempoMap(
            new List<TempoChange> { new(Rational.Zero, 120.0) },
            new List<TimeSignatureChange> { new(Rational.Zero, TimeSignature.CommonTime) }
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
        var events = new List<IPerformanceEvent>
        {
            new SymbolicNoteEvent(MidiNote.C4, Rational.Zero, Rational.Create(1, 4), Velocity.MezzoForte),
        };

        var tempoMap = new TempoMap(
            new List<TempoChange> { new(Rational.Zero, 120.0) },
            new List<TimeSignatureChange> { new(Rational.Zero, TimeSignature.CommonTime) }
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
        var events = new List<IPerformanceEvent>
        {
            new SymbolicNoteEvent(MidiNote.C4, Rational.Zero, Rational.Create(1, 4), Velocity.MezzoForte),
        };

        var tempoMap = new TempoMap(
            new List<TempoChange> { new(Rational.Zero, 120.0) },
            new List<TimeSignatureChange> { new(Rational.Zero, TimeSignature.CommonTime) }
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
        var events = new List<IPerformanceEvent>
        {
            new SymbolicNoteEvent(MidiNote.C4, Rational.Zero, Rational.Create(1, 4), Velocity.MezzoForte), // C4 = MIDI 60
        };

        var tempoMap = new TempoMap(
            new List<TempoChange> { new(Rational.Zero, 120.0) },
            new List<TimeSignatureChange> { new(Rational.Zero, TimeSignature.CommonTime) }
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
            new List<TempoChange> { new(Rational.Zero, 120.0) },
            new List<TimeSignatureChange> { new(Rational.Zero, TimeSignature.CommonTime) }
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
}
