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
    /// Gets a specific measure from the score.
    /// </summary>
    public static Measure GetMeasure(
        this NotationScore score,
        int measureIndex = 0,
        int voiceIndex = 0,
        int partIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(score);
        return score.Parts[partIndex].Voices[voiceIndex].Measures[measureIndex];
    }

    /// <summary>
    /// Asserts that a measure has the expected barline types.
    /// </summary>
    public static Measure AssertBarlines(
        this Measure measure,
        BarlineType? expectedStartBarline = null,
        BarlineType? expectedEndBarline = null)
    {
        ArgumentNullException.ThrowIfNull(measure);

        if (expectedStartBarline.HasValue)
        {
            Assert.Equal(expectedStartBarline.Value, measure.StartBarline);
        }
        else
        {
            Assert.Null(measure.StartBarline);
        }

        if (expectedEndBarline.HasValue)
        {
            Assert.Equal(expectedEndBarline.Value, measure.EndBarline);
        }
        else
        {
            Assert.Null(measure.EndBarline);
        }

        return measure;
    }

    /// <summary>
    /// Asserts that a measure has no barlines (both start and end are null).
    /// </summary>
    public static Measure AssertNoBarlines(this Measure measure)
    {
        ArgumentNullException.ThrowIfNull(measure);
        Assert.Null(measure.StartBarline);
        Assert.Null(measure.EndBarline);
        return measure;
    }

    /// <summary>
    /// Asserts that a measure has the expected number of directions.
    /// </summary>
    public static Measure AssertDirectionCount(this Measure measure, int expectedCount)
    {
        ArgumentNullException.ThrowIfNull(measure);
        Assert.Equal(expectedCount, measure.Directions.Count);
        return measure;
    }

    /// <summary>
    /// Asserts that a measure has no directions.
    /// </summary>
    public static Measure AssertNoDirections(this Measure measure)
    {
        ArgumentNullException.ThrowIfNull(measure);
        Assert.Empty(measure.Directions);
        return measure;
    }

    /// <summary>
    /// Asserts that a measure contains a direction with the specified properties.
    /// </summary>
    public static Measure AssertHasDirection(
        this Measure measure,
        DirectionType expectedType,
        string expectedContent,
        Placement? expectedPlacement = null,
        int? expectedBpm = null)
    {
        ArgumentNullException.ThrowIfNull(measure);

        var direction = measure.Directions.FirstOrDefault(d =>
            d.Type == expectedType && d.Content == expectedContent);

        Assert.NotNull(direction);

        if (expectedPlacement.HasValue)
        {
            Assert.Equal(expectedPlacement.Value, direction.Placement);
        }

        if (expectedBpm.HasValue)
        {
            Assert.Equal(expectedBpm.Value, direction.Bpm);
        }

        return measure;
    }

    /// <summary>
    /// Asserts the number of lyric lines in a measure.
    /// </summary>
    public static Measure AssertLyricCount(this Measure measure, int expectedCount)
    {
        ArgumentNullException.ThrowIfNull(measure);
        Assert.Equal(expectedCount, measure.Lyrics.Count);
        return measure;
    }

    /// <summary>
    /// Asserts that a measure has no lyrics.
    /// </summary>
    public static Measure AssertNoLyrics(this Measure measure)
    {
        ArgumentNullException.ThrowIfNull(measure);
        Assert.Empty(measure.Lyrics);
        return measure;
    }

    /// <summary>
    /// Asserts a specific syllable in a lyric line.
    /// </summary>
    public static Measure AssertLyricSyllable(
        this Measure measure,
        int lyricLineIndex,
        int syllableIndex,
        string expectedText,
        LyricSyllableType? expectedType = null)
    {
        ArgumentNullException.ThrowIfNull(measure);

        Assert.True(lyricLineIndex < measure.Lyrics.Count,
            $"Expected lyric line at index {lyricLineIndex}, but only {measure.Lyrics.Count} lyric lines exist");

        var lyricLine = measure.Lyrics[lyricLineIndex];
        Assert.True(syllableIndex < lyricLine.Syllables.Count,
            $"Expected syllable at index {syllableIndex}, but only {lyricLine.Syllables.Count} syllables exist");

        var syllable = lyricLine.Syllables[syllableIndex];
        Assert.Equal(expectedText, syllable.Text);

        if (expectedType.HasValue)
        {
            Assert.Equal(expectedType.Value, syllable.Type);
        }

        return measure;
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
