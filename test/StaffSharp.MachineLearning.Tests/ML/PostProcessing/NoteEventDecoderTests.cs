namespace StaffSharp.MachineLearning.Tests.ML.PostProcessing;

using StaffSharp.MachineLearning.ML.Models;
using StaffSharp.MachineLearning.ML.PostProcessing;
using StaffSharp.MachineLearning.Options;

public sealed class NoteEventDecoderTests
{
    private const int SampleRate = 16000;
    private const int HopSize = 512;
    private const int FrameRate = SampleRate / HopSize; // 31.25 fps
    private static readonly MLTranscriptionOptions _options = new()
    {
        OnsetThreshold = 0.5f,
        OffsetThreshold = 0.5f,
        FrameThreshold = 0.5f,
        MinNoteLengthSeconds = 0.05f,
        MinGapSeconds = 0.05f,
        MinVelocity = 0.1f,
    };

    // Helper method to create test results with correct signature
    private static PolyphonicTranscriptionResult CreateResult(
        float[,] pianoRoll,
        float[,] onsetRoll,
        float[,]? offsetRoll = null,
        float[,]? velocityRoll = null,
        int? frameRate = null,
        int? sampleRate = null)
    {
        var numFrames = pianoRoll.GetLength(0);
        var numKeys = pianoRoll.GetLength(1);

        return new PolyphonicTranscriptionResult(
            PianoRoll: pianoRoll,
            OnsetRoll: onsetRoll,
            OffsetRoll: offsetRoll ?? new float[numFrames, numKeys],
            VelocityRoll: velocityRoll ?? new float[numFrames, numKeys],
            FrameRate: frameRate ?? FrameRate,
            SampleRate: sampleRate ?? SampleRate
        );
    }

    [Fact]
    public void Decode_WithNullResult_ThrowsArgumentNullException()
    {
        // Arrange
        var decoder = new NoteEventDecoder(_options);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => decoder.Decode(null!));
    }

    [Fact]
    public void Decode_WithInvalidPianoRollSize_ThrowsArgumentException()
    {
        // Arrange
        var decoder = new NoteEventDecoder(_options);
        var result = CreateResult(
            pianoRoll: new float[10, 80], // Wrong key count
            onsetRoll: new float[10, 88]
        );

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => decoder.Decode(result));
        Assert.Contains("Piano roll must have 88 keys", ex.Message);
    }

    [Fact]
    public void Decode_WithInvalidOnsetRollSize_ThrowsArgumentException()
    {
        // Arrange
        var decoder = new NoteEventDecoder(_options);
        var result = CreateResult(
            pianoRoll: new float[10, 88],
            onsetRoll: new float[10, 80] // Wrong key count
        );

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => decoder.Decode(result));
        Assert.Contains("Onset roll must have 88 keys", ex.Message);
    }

    [Fact]
    public void Decode_WithInvalidVelocityRollSize_ThrowsArgumentException()
    {
        // Arrange
        var decoder = new NoteEventDecoder(_options);
        var result = new PolyphonicTranscriptionResult(
            PianoRoll: new float[10, 88],
            OnsetRoll: new float[10, 88],
            OffsetRoll: new float[10, 88],
            VelocityRoll: new float[10, 80], // Wrong key count
            FrameRate: FrameRate,
            SampleRate: SampleRate
        );

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => decoder.Decode(result));
        Assert.Contains("Velocity roll must have 88 keys", ex.Message);
    }

    [Fact]
    public void Decode_WithZeroFrameRate_ThrowsArgumentException()
    {
        // Arrange
        var decoder = new NoteEventDecoder(_options);
        var result = CreateResult(
            pianoRoll: new float[10, 88],
            onsetRoll: new float[10, 88],
            frameRate: 0 // Invalid
        );

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => decoder.Decode(result));
        Assert.Contains("Frame rate must be positive", ex.Message);
    }

    [Fact]
    public void Decode_WithEmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var decoder = new NoteEventDecoder(_options);
        var result = CreateResult(
            pianoRoll: new float[0, 88],
            onsetRoll: new float[0, 88]
        );

        // Act
        var notes = decoder.Decode(result);

        // Assert
        Assert.Empty(notes);
    }

    [Fact]
    public void Decode_WithSingleNote_ReturnsOneNoteEvent()
    {
        // Arrange
        var decoder = new NoteEventDecoder(_options);

        // Create a simple test: C4 (middle C, MIDI 60 = key index 39) plays for 10 frames
        const int numFrames = 20;
        const int keyIndex = 39; // C4
        const int expectedMidi = 21 + keyIndex; // 60

        var pianoRoll = new float[numFrames, 88];
        var onsetRoll = new float[numFrames, 88];
        var offsetRoll = new float[numFrames, 88];
        var velocityRoll = new float[numFrames, 88];

        // Onset at frame 5
        onsetRoll[5, keyIndex] = 1.0f;
        velocityRoll[5, keyIndex] = 0.8f;

        // Active from frame 5-14 (10 frames)
        for (int i = 5; i < 15; i++)
        {
            pianoRoll[i, keyIndex] = 1.0f;
        }

        var result = new PolyphonicTranscriptionResult(
            pianoRoll, onsetRoll, offsetRoll, velocityRoll, FrameRate, SampleRate);

        // Act
        var notes = decoder.Decode(result);

        // Assert
        Assert.Single(notes);
        var note = notes[0];
        Assert.Equal(expectedMidi, note.Pitch.Value);
        Assert.Equal(TimeSpan.FromSeconds(5.0 / FrameRate), note.Onset);
        Assert.Equal(TimeSpan.FromSeconds(10.0 / FrameRate), note.Duration);
        Assert.Equal(0.8f, note.Velocity.Value, precision: 3);
    }

    [Fact]
    public void Decode_WithMultipleNotesOnSameKey_ReturnsAllNotes()
    {
        // Arrange
        var decoder = new NoteEventDecoder(_options);
        const int numFrames = 50;
        const int keyIndex = 39; // C4

        var pianoRoll = new float[numFrames, 88];
        var onsetRoll = new float[numFrames, 88];
        var offsetRoll = new float[numFrames, 88];
        var velocityRoll = new float[numFrames, 88];

        // First note: frames 5-14
        onsetRoll[5, keyIndex] = 1.0f;
        velocityRoll[5, keyIndex] = 0.6f;
        for (int i = 5; i < 15; i++)
            pianoRoll[i, keyIndex] = 1.0f;

        // Second note: frames 20-29
        onsetRoll[20, keyIndex] = 1.0f;
        velocityRoll[20, keyIndex] = 0.9f;
        for (int i = 20; i < 30; i++)
            pianoRoll[i, keyIndex] = 1.0f;

        var result = new PolyphonicTranscriptionResult(
            pianoRoll, onsetRoll, offsetRoll, velocityRoll, FrameRate, SampleRate);

        // Act
        var notes = decoder.Decode(result);

        // Assert
        Assert.Equal(2, notes.Count);

        // First note
        Assert.Equal(TimeSpan.FromSeconds(5.0 / FrameRate), notes[0].Onset);
        Assert.Equal(0.6f, notes[0].Velocity.Value, precision: 3);

        // Second note
        Assert.Equal(TimeSpan.FromSeconds(20.0 / FrameRate), notes[1].Onset);
        Assert.Equal(0.9f, notes[1].Velocity.Value, precision: 3);
    }

    [Fact]
    public void Decode_WithChord_ReturnsMultipleSimultaneousNotes()
    {
        // Arrange
        var decoder = new NoteEventDecoder(_options);
        const int numFrames = 20;

        // C major chord: C4 (39), E4 (43), G4 (46)
        var keyIndices = new[] { 39, 43, 46 };

        var pianoRoll = new float[numFrames, 88];
        var onsetRoll = new float[numFrames, 88];
        var offsetRoll = new float[numFrames, 88];
        var velocityRoll = new float[numFrames, 88];

        // All notes start at frame 5 and last 10 frames
        foreach (var keyIndex in keyIndices)
        {
            onsetRoll[5, keyIndex] = 1.0f;
            velocityRoll[5, keyIndex] = 0.7f;
            for (int i = 5; i < 15; i++)
                pianoRoll[i, keyIndex] = 1.0f;
        }

        var result = new PolyphonicTranscriptionResult(
            pianoRoll, onsetRoll, offsetRoll, velocityRoll, FrameRate, SampleRate);

        // Act
        var notes = decoder.Decode(result);

        // Assert
        Assert.Equal(3, notes.Count);

        // All notes should have same onset time
        var expectedOnset = TimeSpan.FromSeconds(5.0 / FrameRate);
        Assert.All(notes, n => Assert.Equal(expectedOnset, n.Onset));

        // Verify pitches
        var pitches = notes.Select(n => (int)n.Pitch.Value).OrderBy(p => p).ToArray();
        Assert.Equal(new[] { 60, 64, 67 }, pitches); // C4, E4, G4
    }

    [Fact]
    public void Decode_WithReArticulation_EndsFirstNoteAndStartsSecond()
    {
        // Arrange
        var decoder = new NoteEventDecoder(_options);
        const int numFrames = 30;
        const int keyIndex = 39; // C4

        var pianoRoll = new float[numFrames, 88];
        var onsetRoll = new float[numFrames, 88];
        var offsetRoll = new float[numFrames, 88];
        var velocityRoll = new float[numFrames, 88];

        // First onset at frame 5
        onsetRoll[5, keyIndex] = 1.0f;
        velocityRoll[5, keyIndex] = 0.6f;
        for (int i = 5; i < 20; i++)
            pianoRoll[i, keyIndex] = 1.0f;

        // Re-articulation: new onset at frame 15 while still active
        onsetRoll[15, keyIndex] = 1.0f;
        velocityRoll[15, keyIndex] = 0.8f;
        // Continues to frame 20

        var result = new PolyphonicTranscriptionResult(
            pianoRoll, onsetRoll, offsetRoll, velocityRoll, FrameRate, SampleRate);

        // Act
        var notes = decoder.Decode(result);

        // Assert
        Assert.Equal(2, notes.Count);

        // First note: frame 5-14 (ended by re-articulation)
        Assert.Equal(TimeSpan.FromSeconds(5.0 / FrameRate), notes[0].Onset);
        Assert.Equal(TimeSpan.FromSeconds(10.0 / FrameRate), notes[0].Duration);
        Assert.Equal(0.6f, notes[0].Velocity.Value, precision: 3);

        // Second note: frame 15-19
        Assert.Equal(TimeSpan.FromSeconds(15.0 / FrameRate), notes[1].Onset);
        Assert.Equal(TimeSpan.FromSeconds(5.0 / FrameRate), notes[1].Duration);
        Assert.Equal(0.8f, notes[1].Velocity.Value, precision: 3);
    }

    [Fact]
    public void Decode_WithThresholdOptions_RespectsOnsetThreshold()
    {
        // Arrange
        var options = _options with
        {
            OnsetThreshold = 0.7f, // High threshold
            FrameThreshold = 0.5f
        };

        var decoder = new NoteEventDecoder(options);

        const int numFrames = 20;
        const int keyIndex = 39;

        var pianoRoll = new float[numFrames, 88];
        var onsetRoll = new float[numFrames, 88];
        var offsetRoll = new float[numFrames, 88];
        var velocityRoll = new float[numFrames, 88];

        // Weak onset (below threshold)
        onsetRoll[5, keyIndex] = 0.6f;
        velocityRoll[5, keyIndex] = 0.8f;
        for (int i = 5; i < 15; i++)
            pianoRoll[i, keyIndex] = 1.0f;

        var result = new PolyphonicTranscriptionResult(
            pianoRoll, onsetRoll, offsetRoll, velocityRoll, FrameRate, SampleRate);

        // Act
        var notes = decoder.Decode(result);

        // Assert - note should not be detected due to low onset probability
        Assert.Empty(notes);
    }

    [Fact]
    public void Decode_WithThresholdOptions_RespectsFrameThreshold()
    {
        // Arrange
        var options = _options with
        {
            OnsetThreshold = 0.5f,
            FrameThreshold = 0.8f // High threshold
        };
        var decoder = new NoteEventDecoder(options);

        const int numFrames = 20;
        const int keyIndex = 39;

        var pianoRoll = new float[numFrames, 88];
        var onsetRoll = new float[numFrames, 88];
        var offsetRoll = new float[numFrames, 88];
        var velocityRoll = new float[numFrames, 88];

        // Strong onset
        onsetRoll[5, keyIndex] = 1.0f;
        velocityRoll[5, keyIndex] = 0.8f;

        // Weak frame activations (below threshold)
        for (int i = 5; i < 15; i++)
            pianoRoll[i, keyIndex] = 0.7f;

        var result = new PolyphonicTranscriptionResult(
            pianoRoll, onsetRoll, offsetRoll, velocityRoll, FrameRate, SampleRate);

        // Act
        var notes = decoder.Decode(result);

        // Assert - note should be very short or filtered out
        // The onset creates a note, but low frame activations end it immediately
        Assert.True(notes.Count <= 1);
        if (notes.Count == 1)
        {
            Assert.True(notes[0].Duration.TotalSeconds < 0.1);
        }
    }

    [Fact]
    public void Decode_WithMinNoteLengthFilter_FiltersShortNotes()
    {
        // Arrange
        var options = _options with
        {
            MinNoteLengthSeconds = 0.1f // 100ms minimum
        };
        var decoder = new NoteEventDecoder(options);

        const int numFrames = 20;
        const int keyIndex = 39;

        var pianoRoll = new float[numFrames, 88];
        var onsetRoll = new float[numFrames, 88];
        var offsetRoll = new float[numFrames, 88];
        var velocityRoll = new float[numFrames, 88];

        // Very short note (2 frames = ~64ms at 31.25 fps)
        onsetRoll[5, keyIndex] = 1.0f;
        velocityRoll[5, keyIndex] = 0.8f;
        pianoRoll[5, keyIndex] = 1.0f;
        pianoRoll[6, keyIndex] = 1.0f;

        var result = new PolyphonicTranscriptionResult(
            pianoRoll, onsetRoll, offsetRoll, velocityRoll, FrameRate, SampleRate);

        // Act
        var notes = decoder.Decode(result);

        // Assert - note should be filtered out
        Assert.Empty(notes);
    }

    [Fact]
    public void Decode_WithZeroVelocity_FiltersNote()
    {
        // Arrange
        var decoder = new NoteEventDecoder(_options);
        const int numFrames = 20;
        const int keyIndex = 39;

        var pianoRoll = new float[numFrames, 88];
        var onsetRoll = new float[numFrames, 88];
        var offsetRoll = new float[numFrames, 88];
        var velocityRoll = new float[numFrames, 88];

        // Onset with zero velocity
        onsetRoll[5, keyIndex] = 1.0f;
        velocityRoll[5, keyIndex] = 0.0f; // Zero velocity
        for (int i = 5; i < 15; i++)
            pianoRoll[i, keyIndex] = 1.0f;

        var result = new PolyphonicTranscriptionResult(
            pianoRoll, onsetRoll, offsetRoll, velocityRoll, FrameRate, SampleRate);

        // Act
        var notes = decoder.Decode(result);

        // Assert - note should be filtered out
        Assert.Empty(notes);
    }

    [Fact]
    public void Decode_WithNoteActiveAtEnd_CreatesNoteToEndOfAudio()
    {
        // Arrange
        var decoder = new NoteEventDecoder(_options);
        const int numFrames = 20;
        const int keyIndex = 39;

        var pianoRoll = new float[numFrames, 88];
        var onsetRoll = new float[numFrames, 88];
        var offsetRoll = new float[numFrames, 88];
        var velocityRoll = new float[numFrames, 88];

        // Note starts at frame 5 and continues to the end
        onsetRoll[5, keyIndex] = 1.0f;
        velocityRoll[5, keyIndex] = 0.8f;
        for (int i = 5; i < numFrames; i++)
            pianoRoll[i, keyIndex] = 1.0f;

        var result = new PolyphonicTranscriptionResult(
            pianoRoll, onsetRoll, offsetRoll, velocityRoll, FrameRate, SampleRate);

        // Act
        var notes = decoder.Decode(result);

        // Assert
        Assert.Single(notes);
        Assert.Equal(TimeSpan.FromSeconds(5.0 / FrameRate), notes[0].Onset);
        Assert.Equal(TimeSpan.FromSeconds(15.0 / FrameRate), notes[0].Duration);
    }

    [Fact]
    public void Decode_ReturnsNotesSortedByOnset()
    {
        // Arrange
        var decoder = new NoteEventDecoder(_options);
        const int numFrames = 40;

        var pianoRoll = new float[numFrames, 88];
        var onsetRoll = new float[numFrames, 88];
        var offsetRoll = new float[numFrames, 88];
        var velocityRoll = new float[numFrames, 88];

        // Create notes in non-chronological key order
        var noteSpecs = new[]
        {
            (KeyIndex: 50, OnsetFrame: 20),
            (KeyIndex: 40, OnsetFrame: 5),
            (KeyIndex: 60, OnsetFrame: 15)
        };

        foreach (var (keyIndex, onsetFrame) in noteSpecs)
        {
            onsetRoll[onsetFrame, keyIndex] = 1.0f;
            velocityRoll[onsetFrame, keyIndex] = 0.7f;
            for (int i = onsetFrame; i < onsetFrame + 5; i++)
                pianoRoll[i, keyIndex] = 1.0f;
        }

        var result = new PolyphonicTranscriptionResult(
            pianoRoll, onsetRoll, offsetRoll, velocityRoll, FrameRate, SampleRate);

        // Act
        var notes = decoder.Decode(result);

        // Assert
        Assert.Equal(3, notes.Count);

        // Verify sorted by onset time
        Assert.Equal(TimeSpan.FromSeconds(5.0 / FrameRate), notes[0].Onset);
        Assert.Equal(TimeSpan.FromSeconds(15.0 / FrameRate), notes[1].Onset);
        Assert.Equal(TimeSpan.FromSeconds(20.0 / FrameRate), notes[2].Onset);
    }

    [Fact]
    public void Decode_WithVelocityOutOfRange_ClampsToValidRange()
    {
        // Arrange
        var decoder = new NoteEventDecoder(_options);
        const int numFrames = 20;
        const int keyIndex = 39;

        var pianoRoll = new float[numFrames, 88];
        var onsetRoll = new float[numFrames, 88];
        var offsetRoll = new float[numFrames, 88];
        var velocityRoll = new float[numFrames, 88];

        // Velocity exceeding valid range
        onsetRoll[5, keyIndex] = 1.0f;
        velocityRoll[5, keyIndex] = 1.5f; // Invalid, should be clamped to 1.0
        for (int i = 5; i < 15; i++)
            pianoRoll[i, keyIndex] = 1.0f;

        var result = new PolyphonicTranscriptionResult(
            pianoRoll, onsetRoll, offsetRoll, velocityRoll, FrameRate, SampleRate);

        // Act
        var notes = decoder.Decode(result);

        // Assert
        Assert.Single(notes);
        Assert.Equal(1.0f, notes[0].Velocity.Value); // Clamped to max
    }

    [Fact]
    public void Decode_WithAllPianoKeys_HandlesFullRange()
    {
        // Arrange
        var decoder = new NoteEventDecoder(_options);
        const int numFrames = 20;

        var pianoRoll = new float[numFrames, 88];
        var onsetRoll = new float[numFrames, 88];
        var offsetRoll = new float[numFrames, 88];
        var velocityRoll = new float[numFrames, 88];

        // Create a note on every piano key
        for (int keyIndex = 0; keyIndex < 88; keyIndex++)
        {
            onsetRoll[5, keyIndex] = 1.0f;
            velocityRoll[5, keyIndex] = 0.7f;
            for (int i = 5; i < 15; i++)
                pianoRoll[i, keyIndex] = 1.0f;
        }

        var result = new PolyphonicTranscriptionResult(
            pianoRoll, onsetRoll, offsetRoll, velocityRoll, FrameRate, SampleRate);

        // Act
        var notes = decoder.Decode(result);

        // Assert
        Assert.Equal(88, notes.Count);

        // Verify MIDI range (21 to 108)
        var midiNotes = notes.Select(n => (int)n.Pitch.Value).OrderBy(m => m).ToArray();
        Assert.Equal(21, midiNotes.First()); // A0
        Assert.Equal(108, midiNotes.Last());  // C8
    }
}
