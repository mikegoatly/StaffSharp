namespace StaffSharp.TestHelpers;

using StaffSharp.Notation;
using Xunit;

/// <summary>
/// Base class for tests that work with NotationScore objects.
/// Provides common assertion and navigation helpers.
/// </summary>
public abstract class ScoreTestBase
{
    /// <summary>
    /// Gets a part from the score.
    /// </summary>
    protected static Part GetPart(NotationScore score, int partIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(score);
        Assert.True(partIndex < score.Parts.Count, $"Part index {partIndex} out of range. Score has {score.Parts.Count} parts.");
        return score.Parts[partIndex];
    }

    /// <summary>
    /// Gets a voice from the score.
    /// </summary>
    protected static Voice GetVoice(NotationScore score, int voiceIndex = 0, int partIndex = 0)
    {
        var part = GetPart(score, partIndex);
        Assert.True(voiceIndex < part.Voices.Count, $"Voice index {voiceIndex} out of range. Part has {part.Voices.Count} voices.");
        return part.Voices[voiceIndex];
    }

    /// <summary>
    /// Gets a measure from the score.
    /// </summary>
    protected static Measure GetMeasure(NotationScore score, int measureIndex = 0, int voiceIndex = 0, int partIndex = 0)
    {
        var voice = GetVoice(score, voiceIndex, partIndex);
        Assert.True(measureIndex < voice.Measures.Count, $"Measure index {measureIndex} out of range. Voice has {voice.Measures.Count} measures.");
        return voice.Measures[measureIndex];
    }

    /// <summary>
    /// Asserts a voice has the expected properties and returns it for further chaining.
    /// </summary>
    protected static Voice AssertVoice(
        NotationScore score,
        int voiceIndex,
        int expectedNumber,
        int? expectedMeasureCount = null,
        int partIndex = 0)
    {
        var voice = GetVoice(score, voiceIndex, partIndex);
        Assert.Equal(expectedNumber, voice.Number);

        if (expectedMeasureCount.HasValue)
        {
            Assert.Equal(expectedMeasureCount.Value, voice.Measures.Count);
        }

        return voice;
    }

    /// <summary>
    /// Asserts that a note has a specific decoration at the given index.
    /// </summary>
    protected static void AssertDecoration(NotationNote note, Decoration expectedDecoration, int decorationIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(note);
        Assert.True(decorationIndex < note.Decorations.Count,
            $"Decoration index {decorationIndex} out of range. Note has {note.Decorations.Count} decorations.");
        Assert.Equal(expectedDecoration, note.Decorations[decorationIndex]);
    }

    /// <summary>
    /// Asserts that a note has exactly the specified decorations in order.
    /// </summary>
    protected static void AssertDecorations(NotationNote note, params Decoration[] expectedDecorations)
    {
        ArgumentNullException.ThrowIfNull(note);
        ArgumentNullException.ThrowIfNull(expectedDecorations);
        Assert.Equal(expectedDecorations.Length, note.Decorations.Count);

        for (int i = 0; i < expectedDecorations.Length; i++)
        {
            Assert.Equal(expectedDecorations[i], note.Decorations[i]);
        }
    }

    /// <summary>
    /// Asserts that a note has no decorations.
    /// </summary>
    protected static void AssertNoDecorations(NotationNote note)
    {
        ArgumentNullException.ThrowIfNull(note);
        Assert.Empty(note.Decorations);
    }

    /// <summary>
    /// Asserts that a chord has a specific decoration at the given index.
    /// </summary>
    protected static void AssertDecoration(Chord chord, Decoration expectedDecoration, int decorationIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(chord);
        Assert.True(decorationIndex < chord.Decorations.Count,
            $"Decoration index {decorationIndex} out of range. Chord has {chord.Decorations.Count} decorations.");
        Assert.Equal(expectedDecoration, chord.Decorations[decorationIndex]);
    }

    /// <summary>
    /// Asserts the score has the expected number of parts.
    /// </summary>
    protected static void AssertPartCount(NotationScore score, int expectedCount)
    {
        ArgumentNullException.ThrowIfNull(score);
        Assert.Equal(expectedCount, score.Parts.Count);
    }

    /// <summary>
    /// Asserts the score has the expected number of voices in the first part.
    /// </summary>
    protected static void AssertVoiceCount(NotationScore score, int expectedCount, int partIndex = 0)
    {
        var part = GetPart(score, partIndex);
        Assert.Equal(expectedCount, part.Voices.Count);
    }

    /// <summary>
    /// Gets all notes from a specific measure.
    /// </summary>
    protected static IReadOnlyList<NotationNote> GetNotes(NotationScore score, int measureIndex = 0, int voiceIndex = 0, int partIndex = 0)
    {
        return GetMeasure(score, measureIndex, voiceIndex, partIndex)
            .Events
            .OfType<NotationNote>()
            .ToList();
    }

    /// <summary>
    /// Gets all chords from a specific measure.
    /// </summary>
    protected static IReadOnlyList<Chord> GetChords(NotationScore score, int measureIndex = 0, int voiceIndex = 0, int partIndex = 0)
    {
        return GetMeasure(score, measureIndex, voiceIndex, partIndex)
            .Events
            .OfType<Chord>()
            .ToList();
    }

    /// <summary>
    /// Gets all rests from a specific measure.
    /// </summary>
    protected static IReadOnlyList<Rest> GetRests(NotationScore score, int measureIndex = 0, int voiceIndex = 0, int partIndex = 0)
    {
        return GetMeasure(score, measureIndex, voiceIndex, partIndex)
            .Events
            .OfType<Rest>()
            .ToList();
    }

    /// <summary>
    /// Gets all slurs from a specific measure.
    /// </summary>
    protected static IReadOnlyList<Slur> GetSlurs(NotationScore score, int measureIndex = 0, int voiceIndex = 0, int partIndex = 0)
    {
        return GetMeasure(score, measureIndex, voiceIndex, partIndex).Slurs;
    }
}
