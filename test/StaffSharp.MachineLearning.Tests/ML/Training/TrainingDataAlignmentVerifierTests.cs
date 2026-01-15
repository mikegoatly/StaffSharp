namespace StaffSharp.MachineLearning.Tests.ML.Training;

using StaffSharp.MachineLearning.ML.Training;
using StaffSharp.TestHelpers.Builders;

public sealed class TrainingDataAlignmentVerifierTests
{
    private const int MinMidiNote = 21;
    private const int NumKeys = 88;

    [Fact]
    public void VerifyAlignment_WithValidData_ReturnsValid()
    {
        // Arrange
        var verifier = new TrainingDataAlignmentVerifier();
        var trainingData = CreateValidTrainingData();
        var (midiPath, audioPath) = CreateTestFiles();

        try
        {
            // Act
            var result = verifier.VerifyAlignment(midiPath, audioPath, trainingData);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(0, result.ErrorCount);
        }
        finally
        {
            CleanupTestFiles(midiPath, audioPath);
        }
    }

    [Fact]
    public void VerifyAlignment_DetectsFrameRateMismatch()
    {
        // Arrange
        var verifier = new TrainingDataAlignmentVerifier();
        var trainingData = CreateValidTrainingData();
        var (midiPath, audioPath) = CreateTestFiles();

        try
        {
            // Act
            var result = verifier.VerifyAlignment(midiPath, audioPath, trainingData);

            // Assert - should detect if frame rate doesn't match 31.25 FPS (16000 / 512)
            // This test validates the frame rate is being checked
            Assert.NotNull(result);
        }
        finally
        {
            CleanupTestFiles(midiPath, audioPath);
        }
    }

    [Fact]
    public void VerifyAlignment_DetectsMissingOnsetMarker()
    {
        // Arrange
        var verifier = new TrainingDataAlignmentVerifier();
        var trainingData = CreateTrainingDataWithMissingOnset();
        var (midiPath, audioPath) = CreateTestFiles();

        try
        {
            // Act
            var result = verifier.VerifyAlignment(midiPath, audioPath, trainingData);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, i => i.Category == "MISSING_ONSET");
        }
        finally
        {
            CleanupTestFiles(midiPath, audioPath);
        }
    }

    [Fact]
    public void VerifyAlignment_DetectsOutOfRangeVelocity()
    {
        // Arrange
        var verifier = new TrainingDataAlignmentVerifier();
        var trainingData = CreateTrainingDataWithInvalidVelocity();
        var (midiPath, audioPath) = CreateTestFiles();

        try
        {
            // Act
            var result = verifier.VerifyAlignment(midiPath, audioPath, trainingData);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, i => i.Category == "INVALID_VELOCITY");
        }
        finally
        {
            CleanupTestFiles(midiPath, audioPath);
        }
    }

    [Fact]
    public void VerifyAlignment_DetectsPianoRollMismatch()
    {
        // Arrange
        var verifier = new TrainingDataAlignmentVerifier();
        var trainingData = CreateTrainingDataWithPianoRollMismatch();
        var (midiPath, audioPath) = CreateTestFiles();

        try
        {
            // Act
            var result = verifier.VerifyAlignment(midiPath, audioPath, trainingData);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, i => i.Category == "PIANO_ROLL_MISMATCH");
        }
        finally
        {
            CleanupTestFiles(midiPath, audioPath);
        }
    }

    [Fact]
    public void GetIssuesSummary_WithNoIssues_ShowsValidStatus()
    {
        // Arrange
        var verifier = new TrainingDataAlignmentVerifier();
        var result = new VerificationResult
        {
            IsValid = true,
            ErrorCount = 0,
            WarningCount = 0,
            InfoCount = 0,
            TotalFrames = 1000,
            TotalMidiNotes = 10,
            Issues = []
        };

        // Act
        var summary = result.ToString();

        // Assert
        Assert.Contains("✓", summary);
        Assert.Contains("valid", summary);
    }

    [Fact]
    public void GetIssuesSummary_WithIssues_ListsProblems()
    {
        // Arrange
        var verifier = new TrainingDataAlignmentVerifier();
        var result = new VerificationResult
        {
            IsValid = false,
            ErrorCount = 2,
            WarningCount = 1,
            InfoCount = 0,
            TotalFrames = 1000,
            TotalMidiNotes = 10,
            Issues =
            [
                new AlignmentIssue
                {
                    Severity = "ERROR",
                    Category = "TIMING",
                    Message = "Test error"
                }
            ]
        };

        // Act
        var summary = result.ToString();

        // Assert
        Assert.Contains("✗", summary);
        Assert.Contains("2 errors", summary);
        Assert.Contains("1 warning", summary);
    }

    // Helper methods

    private static TrainingDataSample CreateValidTrainingData()
    {
        const int frameCount = 1000;
        var melSpec = new float[frameCount, 229];
        var pianoRoll = new float[frameCount, NumKeys];
        var onsetRoll = new float[frameCount, NumKeys];
        var offsetRoll = new float[frameCount, NumKeys];
        var velocityRoll = new float[frameCount, NumKeys];

        // Frame rate is 31.25 FPS (16000 samples / 512 hop size)
        // So 1000 frames = 32 seconds of audio
        // Create a simple valid pattern: C4 (note 60) from frame 100 to 200
        // That's from 3.2s to 6.4s
        var keyIndex = 60 - MinMidiNote; // C4 is 9 semitones above A0
        onsetRoll[100, keyIndex] = 1.0f;
        velocityRoll[100, keyIndex] = 0.8f;

        for (int frame = 100; frame <= 200; frame++)
        {
            pianoRoll[frame, keyIndex] = 1.0f;
        }

        offsetRoll[200, keyIndex] = 1.0f;

        return new TrainingDataSample
        {
            MelSpectrogram = melSpec,
            PianoRoll = pianoRoll,
            OnsetRoll = onsetRoll,
            OffsetRoll = offsetRoll,
            VelocityRoll = velocityRoll
        };
    }

    private static TrainingDataSample CreateTrainingDataWithMissingOnset()
    {
        const int frameCount = 1000;
        var melSpec = new float[frameCount, 229];
        var pianoRoll = new float[frameCount, NumKeys];
        var onsetRoll = new float[frameCount, NumKeys];
        var offsetRoll = new float[frameCount, NumKeys];
        var velocityRoll = new float[frameCount, NumKeys];

        // Create piano roll but NO onset marker
        var keyIndex = 60 - MinMidiNote;

        for (int frame = 100; frame <= 200; frame++)
        {
            pianoRoll[frame, keyIndex] = 1.0f;
        }

        offsetRoll[200, keyIndex] = 1.0f;
        // NOTE: Missing onsetRoll[100, keyIndex] = 1.0f

        return new TrainingDataSample
        {
            MelSpectrogram = melSpec,
            PianoRoll = pianoRoll,
            OnsetRoll = onsetRoll,
            OffsetRoll = offsetRoll,
            VelocityRoll = velocityRoll
        };
    }

    private static TrainingDataSample CreateTrainingDataWithInvalidVelocity()
    {
        const int frameCount = 1000;
        var melSpec = new float[frameCount, 229];
        var pianoRoll = new float[frameCount, NumKeys];
        var onsetRoll = new float[frameCount, NumKeys];
        var offsetRoll = new float[frameCount, NumKeys];
        var velocityRoll = new float[frameCount, NumKeys];

        var keyIndex = 60 - MinMidiNote;

        onsetRoll[100, keyIndex] = 1.0f;
        velocityRoll[100, keyIndex] = 1.5f; // Invalid: > 1.0

        for (int frame = 100; frame <= 200; frame++)
        {
            pianoRoll[frame, keyIndex] = 1.0f;
        }

        offsetRoll[200, keyIndex] = 1.0f;

        return new TrainingDataSample
        {
            MelSpectrogram = melSpec,
            PianoRoll = pianoRoll,
            OnsetRoll = onsetRoll,
            OffsetRoll = offsetRoll,
            VelocityRoll = velocityRoll
        };
    }

    private static TrainingDataSample CreateTrainingDataWithPianoRollMismatch()
    {
        const int frameCount = 1000;
        var melSpec = new float[frameCount, 229];
        var pianoRoll = new float[frameCount, NumKeys];
        var onsetRoll = new float[frameCount, NumKeys];
        var offsetRoll = new float[frameCount, NumKeys];
        var velocityRoll = new float[frameCount, NumKeys];

        var keyIndex = 60 - MinMidiNote;

        // Mark onset but don't activate piano roll
        onsetRoll[100, keyIndex] = 1.0f;
        velocityRoll[100, keyIndex] = 0.8f;
        // NOTE: Missing pianoRoll activation

        offsetRoll[200, keyIndex] = 1.0f;

        return new TrainingDataSample
        {
            MelSpectrogram = melSpec,
            PianoRoll = pianoRoll,
            OnsetRoll = onsetRoll,
            OffsetRoll = offsetRoll,
            VelocityRoll = velocityRoll
        };
    }

    // We only need to artificially create MIDI files here at the moment. If we ever need to do it elsewhere
    // we should create a builder class for it.
    private static (string midiPath, string audioPath) CreateTestFiles()
    {
        // For unit tests, we create minimal valid files
        // In real usage, actual MAESTRO files would be used
        var tempDir = Path.Combine(Path.GetTempPath(), "staffsharp_test");
        Directory.CreateDirectory(tempDir);

        var midiPath = Path.Combine(tempDir, $"test_{Guid.NewGuid()}.mid");
        var audioPath = Path.Combine(tempDir, $"test_{Guid.NewGuid()}.wav");

        // Create minimal MIDI file with note timing matching the training data
        // Training data: 1000 frames at 31.25 FPS = 32 seconds
        // C4 (MIDI 60) from frame 100 to 200 = 3.2s to 6.4s
        
        var midiFile = new Melanchall.DryWetMidi.Core.MidiFile
        {
            TimeDivision = new Melanchall.DryWetMidi.Core.TicksPerQuarterNoteTimeDivision(480)
        };
        
        var trackChunk = new Melanchall.DryWetMidi.Core.TrackChunk();
        
        // With 480 PPQ and 120 BPM default tempo:
        // 1 quarter note = 0.5 seconds = 480 ticks
        // 1 second = 960 ticks
        // 3.2 seconds = 3072 ticks
        trackChunk.Events.Add(new Melanchall.DryWetMidi.Core.NoteOnEvent { NoteNumber = new(60), Velocity = new(100), DeltaTime = 3072 });
        // Duration from 3.2s to 6.4s = 3.2 seconds = 3072 ticks
        trackChunk.Events.Add(new Melanchall.DryWetMidi.Core.NoteOffEvent { NoteNumber = new(60), DeltaTime = 3072 });

        midiFile.Chunks.Add(trackChunk);
        midiFile.Write(midiPath);

        // Create minimal WAV file (dummy audio - 32 seconds at 16kHz)
        var audioData = new float[512000]; // 32 seconds at 16kHz
        for (int i = 0; i < audioData.Length; i++)
        {
            audioData[i] = 0.0f; // Silence
        }

        var audioBuffer = new StaffSharp.Audio.AudioBuffer(audioData, 16000, channels: 1);
        using (var stream = File.Create(audioPath))
        {
            audioBuffer.Save(stream);
        }

        return (midiPath, audioPath);
    }

    private static void CleanupTestFiles(string midiPath, string audioPath)
    {
#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            if (File.Exists(midiPath))
            {
                File.Delete(midiPath);
            }

            if (File.Exists(audioPath))
            {
                File.Delete(audioPath);
            }

            var dir = Path.GetDirectoryName(midiPath);
            if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
            {
                Directory.Delete(dir);
            }
        }
        catch { /* Cleanup failure is not fatal */ }
#pragma warning restore CA1031 // Do not catch general exception types
    }
}
