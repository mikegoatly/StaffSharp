namespace StaffSharp.TestHelpers;

using StaffSharp;

using StaffSharp.Notation;

using Xunit;

/// <summary>
/// Helper methods for asserting on NotationScore structures in tests.
/// </summary>
public static class ScoreAssert
{
    /// <summary>
    /// Gets events from a specific measure (defaults to first part, first voice, first measure).
    /// </summary>
    public static IReadOnlyList<INotationEvent> GetEvents(
        this NotationScore score,
        int measureIndex = 0,
        int voiceIndex = 0,
        int partIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(score);
        return score.Parts[partIndex].Voices[voiceIndex].Measures[measureIndex].Events;
    }

    /// <summary>
    /// Gets only notes from a specific measure.
    /// </summary>
    public static IReadOnlyList<NotationNote> GetNotes(
        this NotationScore score,
        int measureIndex = 0,
        int voiceIndex = 0,
        int partIndex = 0)
    {
        return score.GetEvents(measureIndex, voiceIndex, partIndex)
            .OfType<NotationNote>()
            .ToList();
    }

    /// <summary>
    /// Gets only chords from a specific measure.
    /// </summary>
    public static IReadOnlyList<Chord> GetChords(
        this NotationScore score,
        int measureIndex = 0,
        int voiceIndex = 0,
        int partIndex = 0)
    {
        return score.GetEvents(measureIndex, voiceIndex, partIndex)
            .OfType<Chord>()
            .ToList();
    }

    /// <summary>
    /// Gets only rests from a specific measure.
    /// </summary>
    public static IReadOnlyList<Rest> GetRests(
        this NotationScore score,
        int measureIndex = 0,
        int voiceIndex = 0,
        int partIndex = 0)
    {
        return score.GetEvents(measureIndex, voiceIndex, partIndex)
            .OfType<Rest>()
            .ToList();
    }

    /// <summary>
    /// Asserts that a note matches expected properties. Returns the note for chaining.
    /// </summary>
    public static NotationNote AssertNote(
        this NotationNote note,
        PitchClass expectedPitchClass,
        SymbolicDuration expectedDuration,
        int? expectedOctave = null,
        TieType? expectedTie = null,
        Tuplet? expectedTuplet = null,
        Accidental? expectedAccidental = null,
        IReadOnlyList<Decoration>? expectedDecorations = null)
    {
        ArgumentNullException.ThrowIfNull(note);
        Assert.Equal(expectedPitchClass, note.Pitch.PitchClass);

        // Compare duration base and dots separately from tuplet
        Assert.Equal(expectedDuration.Base, note.Duration.Base);
        Assert.Equal(expectedDuration.Dots, note.Duration.Dots);

        if (expectedOctave.HasValue)
        {
            Assert.Equal(expectedOctave.Value, note.Pitch.Octave);
        }

        if (expectedTie.HasValue)
        {
            Assert.Equal(expectedTie.Value, note.Tie);
        }

        if (expectedTuplet != null)
        {
            Assert.Equal(expectedTuplet, note.Duration.Tuplet);
        }
        else
        {
            Assert.Null(note.Duration.Tuplet);
        }

        if (expectedAccidental.HasValue)
        {
            Assert.Equal(expectedAccidental.Value, note.Pitch.Accidental);
        }

        if (expectedDecorations != null)
        {
            Assert.Equal(expectedDecorations, note.Decorations);
        }

        return note;
    }

    /// <summary>
    /// Asserts that a rest matches expected duration. Returns the rest for chaining.
    /// </summary>
    public static Rest AssertRest(
        this Rest rest,
        SymbolicDuration expectedDuration)
    {
        ArgumentNullException.ThrowIfNull(rest);
        Assert.Equal(expectedDuration, rest.Duration);
        return rest;
    }

    /// <summary>
    /// Asserts that a chord matches expected properties. Returns the chord for chaining.
    /// </summary>
    public static Chord AssertChord(
        this Chord chord,
        IEnumerable<PitchClass> expectedPitchClasses,
        SymbolicDuration expectedDuration,
        TieType? expectedTie = null,
        Tuplet? expectedTuplet = null)
    {
        ArgumentNullException.ThrowIfNull(chord);
        Assert.Equal(expectedPitchClasses, chord.Pitches.Select(p => p.PitchClass));
        Assert.Equal(expectedDuration.Base, chord.Duration.Base);
        Assert.Equal(expectedDuration.Dots, chord.Duration.Dots);

        if (expectedTie.HasValue)
        {
            Assert.Equal(expectedTie.Value, chord.Tie);
        }

        if (expectedTuplet != null)
        {
            Assert.Equal(expectedTuplet, chord.Duration.Tuplet);
        }
        else
        {
            Assert.Null(chord.Duration.Tuplet);
        }

        return chord;
    }

    /// <summary>
    /// Starts a fluent assertion chain for a sequence of events.
    /// </summary>
    public static EventSequenceAssertion AssertSequence(
        this NotationScore score,
        int measureIndex = 0,
        int voiceIndex = 0,
        int partIndex = 0)
    {
        var events = score.GetEvents(measureIndex, voiceIndex, partIndex);
        return new EventSequenceAssertion(events);
    }
}

/// <summary>
/// Fluent API for asserting a sequence of events in order.
/// </summary>
public sealed class EventSequenceAssertion
{
    private readonly IReadOnlyList<INotationEvent> _events;
    private int _index;

    public EventSequenceAssertion(IReadOnlyList<INotationEvent> events)
    {
        _events = events;
        _index = 0;
    }

    /// <summary>
    /// Asserts the next event is a note with the specified properties.
    /// </summary>
    public EventSequenceAssertion Note(
        PitchClass pitchClass,
        SymbolicDuration duration,
        int? octave = null,
        TieType? tie = null,
        Tuplet? tuplet = null,
        Accidental? accidental = null)
    {
        Assert.True(_index < _events.Count, $"Expected note at index {_index}, but only {_events.Count} events exist");

        var note = Assert.IsType<NotationNote>(_events[_index]);
        note.AssertNote(pitchClass, duration, octave, tie, tuplet, accidental);

        _index++;
        return this;
    }

    /// <summary>
    /// Asserts the next event is a rest with the specified duration.
    /// </summary>
    public EventSequenceAssertion Rest(SymbolicDuration duration)
    {
        Assert.True(_index < _events.Count, $"Expected rest at index {_index}, but only {_events.Count} events exist");

        var rest = Assert.IsType<Rest>(_events[_index]);
        rest.AssertRest(duration);

        _index++;
        return this;
    }

    /// <summary>
    /// Asserts the next event is a chord with the specified properties.
    /// </summary>
    public EventSequenceAssertion Chord(
        IEnumerable<PitchClass> pitchClasses,
        SymbolicDuration duration,
        TieType? tie = null,
        Tuplet? tuplet = null)
    {
        Assert.True(_index < _events.Count, $"Expected chord at index {_index}, but only {_events.Count} events exist");

        var chord = Assert.IsType<Chord>(_events[_index]);
        chord.AssertChord(pitchClasses, duration, tie, tuplet);

        _index++;
        return this;
    }

    /// <summary>
    /// Asserts that there are no more events after those already asserted.
    /// </summary>
    public void AndNoMore()
    {
        Assert.Equal(_index, _events.Count);
    }

    /// <summary>
    /// Asserts the total count of events matches the expected count.
    /// </summary>
    public EventSequenceAssertion HasCount(int expectedCount)
    {
        Assert.Equal(expectedCount, _events.Count);
        return this;
    }
}
