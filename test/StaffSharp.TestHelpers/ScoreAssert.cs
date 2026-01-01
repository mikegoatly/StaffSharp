namespace StaffSharp.TestHelpers;

using System.Globalization;

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
    /// Starts a fluent assertion chain for a grand staff.
    /// </summary>
    public static GrandStaffAssertion AssertGrandStaff(
        this NotationScore score,
        int partIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(score);
        return new GrandStaffAssertion(score.Parts[partIndex]);
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

/// <summary>
/// Fluent API for asserting grand staff properties with detailed diagnostics.
/// </summary>
public sealed class GrandStaffAssertion
{
    private readonly Part _part;

    public GrandStaffAssertion(Part part)
    {
        _part = part;
    }

    /// <summary>
    /// Asserts that the part is configured as a grand staff.
    /// </summary>
    public GrandStaffAssertion IsGrandStaff()
    {
        Assert.True(_part.IsGrandStaff, "Expected part to be a grand staff");
        Assert.Equal(2, _part.Staves.Count);
        return this;
    }

    /// <summary>
    /// Asserts that the part is NOT configured as a grand staff.
    /// </summary>
    public GrandStaffAssertion IsNotGrandStaff()
    {
        Assert.False(_part.IsGrandStaff, "Expected part to NOT be a grand staff");
        Assert.Single(_part.Staves);
        return this;
    }

    /// <summary>
    /// Asserts the clefs are correctly set for treble and bass staves.
    /// </summary>
    public GrandStaffAssertion HasStandardClefs()
    {
        Assert.Equal(Clef.Treble, _part.Staves[0].Clef);
        Assert.Equal(Clef.Bass, _part.Staves[1].Clef);
        return this;
    }

    /// <summary>
    /// Asserts that notes are split correctly between treble and bass staves with detailed diagnostics.
    /// </summary>
    public GrandStaffAssertion HasNotesSplitAt(int splitPoint, params (int midiNote, string expectedStaff)[] expectedNotes)
    {
        IsGrandStaff();

        var trebleEvents = _part.Staves[0].Voices
            .SelectMany(v => v.Measures)
            .SelectMany(m => m.Events)
            .OfType<NotationNote>()
            .ToList();

        var bassEvents = _part.Staves[1].Voices
            .SelectMany(v => v.Measures)
            .SelectMany(m => m.Events)
            .OfType<NotationNote>()
            .ToList();

        // Build diagnostic message
        var diagnostics = new System.Text.StringBuilder();
        diagnostics.AppendLine(CultureInfo.InvariantCulture, $"Grand Staff Note Split Analysis (split point: MIDI {splitPoint})");
        diagnostics.AppendLine(CultureInfo.InvariantCulture, $"Expected: Notes >= {splitPoint} ? Treble, Notes < {splitPoint} ? Bass");
        diagnostics.AppendLine();
        
        diagnostics.AppendLine(CultureInfo.InvariantCulture, $"Treble Staff ({trebleEvents.Count} notes):");
        foreach (var note in trebleEvents)
        {
            var midi = GetMidiNumber(note.Pitch);
            diagnostics.AppendLine(CultureInfo.InvariantCulture, $"  - {note.Pitch.PitchClass}{note.Pitch.Octave} (MIDI {midi})");
        }
        
        diagnostics.AppendLine();
        diagnostics.AppendLine(CultureInfo.InvariantCulture, $"Bass Staff ({bassEvents.Count} notes):");
        foreach (var note in bassEvents)
        {
            var midi = GetMidiNumber(note.Pitch);
            diagnostics.AppendLine(CultureInfo.InvariantCulture, $"  - {note.Pitch.PitchClass}{note.Pitch.Octave} (MIDI {midi})");
        }

        diagnostics.AppendLine();
        diagnostics.AppendLine("Expected Distribution:");
        var expectedTrebleCount = expectedNotes.Count(n => n.expectedStaff == "treble");
        var expectedBassCount = expectedNotes.Count(n => n.expectedStaff == "bass");
        
        foreach (var (midi, staff) in expectedNotes)
        {
            diagnostics.AppendLine(CultureInfo.InvariantCulture, $"  - MIDI {midi} ? {staff}");
        }

        diagnostics.AppendLine();
        diagnostics.AppendLine(CultureInfo.InvariantCulture, $"Expected: {expectedTrebleCount} treble, {expectedBassCount} bass");
        diagnostics.AppendLine(CultureInfo.InvariantCulture, $"Actual:   {trebleEvents.Count} treble, {bassEvents.Count} bass");

        // Perform assertions with diagnostic output
        Assert.True(
            trebleEvents.Count == expectedTrebleCount,
            $"Treble staff note count mismatch.\n{diagnostics}");

        Assert.True(
            bassEvents.Count == expectedBassCount,
            $"Bass staff note count mismatch.\n{diagnostics}");

        // Verify each note is on the correct staff
        foreach (var (midi, expectedStaff) in expectedNotes)
        {
            var isInTreble = trebleEvents.Any(n => GetMidiNumber(n.Pitch) == midi);
            var isInBass = bassEvents.Any(n => GetMidiNumber(n.Pitch) == midi);

            if (expectedStaff == "treble")
            {
                Assert.True(isInTreble, 
                    $"Expected MIDI {midi} on treble staff but it was not found.\n{diagnostics}");
            }
            else
            {
                Assert.True(isInBass, 
                    $"Expected MIDI {midi} on bass staff but it was not found.\n{diagnostics}");
            }
        }

        return this;
    }

    /// <summary>
    /// Asserts the event counts on treble and bass staves (includes all event types: notes, rests, chords).
    /// </summary>
    public GrandStaffAssertion HasEventCounts(int expectedTrebleCount, int expectedBassCount)
    {
        IsGrandStaff();

        var trebleEvents = _part.Staves[0].Voices
            .SelectMany(v => v.Measures)
            .SelectMany(m => m.Events)
            .ToList();

        var bassEvents = _part.Staves[1].Voices
            .SelectMany(v => v.Measures)
            .SelectMany(m => m.Events)
            .ToList();

        var diagnostics = new System.Text.StringBuilder();
        diagnostics.AppendLine("Grand Staff Event Count Analysis");
        diagnostics.AppendLine(CultureInfo.InvariantCulture, $"Treble Staff: {trebleEvents.Count} events");
        foreach (var evt in trebleEvents)
        {
            diagnostics.AppendLine(CultureInfo.InvariantCulture, $"  - {evt.GetType().Name}");
        }
        
        diagnostics.AppendLine(CultureInfo.InvariantCulture, $"Bass Staff: {bassEvents.Count} events");
        foreach (var evt in bassEvents)
        {
            diagnostics.AppendLine(CultureInfo.InvariantCulture, $"  - {evt.GetType().Name}");
        }

        Assert.True(
            trebleEvents.Count == expectedTrebleCount,
            $"Expected {expectedTrebleCount} treble events but got {trebleEvents.Count}\n{diagnostics}");

        Assert.True(
            bassEvents.Count == expectedBassCount,
            $"Expected {expectedBassCount} bass events but got {bassEvents.Count}\n{diagnostics}");

        return this;
    }

    /// <summary>
    /// Asserts that the bass staff is empty (no measures with events).
    /// </summary>
    public GrandStaffAssertion HasEmptyBassStaff()
    {
        IsGrandStaff();
        
        var bassStaff = _part.Staves[1];
        Assert.Single(bassStaff.Voices);
        Assert.Empty(bassStaff.Voices[0].Measures);
        
        return this;
    }

    private static int GetMidiNumber(Pitch pitch)
    {
        // Calculate MIDI number from pitch class and octave
        return (pitch.Octave + 1) * 12 + (int)pitch.PitchClass;
    }
}
