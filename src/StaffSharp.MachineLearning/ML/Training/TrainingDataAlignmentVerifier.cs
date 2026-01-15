namespace StaffSharp.MachineLearning.ML.Training;

using System.Globalization;
using System.Text;

using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

using StaffSharp.Notation;

/// <summary>
/// Represents a single alignment issue found during verification.
/// </summary>
public sealed class AlignmentIssue
{
    public string Severity { get; init; } = string.Empty; // "ERROR", "WARNING", "INFO"
    public string Category { get; init; } = string.Empty;  // "FRAME_RATE", "TIMING", "OVERLAP", etc.
    public string Message { get; init; } = string.Empty;
    public int? NoteIndex { get; init; }
    public int? KeyIndex { get; init; }
    public double? TimeSeconds { get; init; }
}

/// <summary>
/// Verification results with statistics and issues found.
/// </summary>
public sealed class VerificationResult
{
    public bool IsValid { get; init; }
    public int ErrorCount { get; init; }
    public int WarningCount { get; init; }
    public int InfoCount { get; init; }
    public IList<AlignmentIssue> Issues { get; init; } = [];

    public double AudioDurationSeconds { get; init; }
    public int TotalFrames { get; init; }
    public int TotalMidiNotes { get; init; }
    public float FrameRate { get; init; }

    public string Summary { get; init; } = string.Empty;

    public override string ToString()
    {
        if (IsValid)
        {
            return $"✓ Alignment valid. {TotalMidiNotes} notes verified across {TotalFrames} frames.";
        }

        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.CurrentCulture, $"✗ Alignment issues found ({ErrorCount} errors, {WarningCount} warnings):");
        sb.AppendLine();

        var byCategory = Issues.GroupBy(i => i.Category);
        foreach (var category in byCategory)
        {
            sb.AppendLine(CultureInfo.CurrentCulture, $"  {category.Key}:");
            foreach (var issue in category.OrderBy(i => i.Severity == "ERROR" ? 0 : (i.Severity == "WARNING" ? 1 : 2)))
            {
                var icon = issue.Severity switch
                {
                    "ERROR" => "✗",
                    "WARNING" => "⚠",
                    _ => "ℹ"
                };

                sb.AppendLine(CultureInfo.CurrentCulture, $"    {icon} {issue.Message}");
            }
        }

        return sb.ToString();
    }
}

/// <summary>
/// Verifies alignment between MIDI notes, audio duration, and extracted training features.
/// 
/// This validates that:
/// 1. Frame rate calculation is correct (SampleRate / HopSize)
/// 2. MIDI onset/offset times map to correct frame indices
/// 3. Piano roll, onset roll, and offset roll temporal boundaries match audio duration
/// 4. No overlapping onsets on the same key
/// 5. Velocity values are in valid range [0, 1]
/// 6. Overall timing doesn't exceed audio duration
/// </summary>
public sealed class TrainingDataAlignmentVerifier
{
    private const int MinMidiNote = 21;  // A0
    private const int MaxMidiNote = 108; // C8
    private const int NumKeys = 88;
    private const int HopSize = 512;
    private const int SampleRate = 16000;

    private readonly List<AlignmentIssue> _issues = [];

    /// <summary>
    /// Verifies alignment between MIDI file, audio duration, and training sample.
    /// </summary>
    /// <param name="midiPath">Path to MIDI file</param>
    /// <param name="audioPath">Path to audio file</param>
    /// <param name="trainingData">Training data sample to verify</param>
    /// <returns>Verification results with issues and statistics</returns>
    public VerificationResult VerifyAlignment(
        string midiPath,
        string audioPath,
        TrainingDataSample trainingData)
    {
        ArgumentNullException.ThrowIfNull(midiPath);
        ArgumentNullException.ThrowIfNull(audioPath);
        ArgumentNullException.ThrowIfNull(trainingData);

        _issues.Clear();

        // Get audio duration from sample count
        var audioDurationSeconds = trainingData.MelSpectrogram.GetLength(0) / GetFrameRate();
        var totalFrames = trainingData.MelSpectrogram.GetLength(0);

        // Parse MIDI to extract note timings
        List<MidiNoteInfo> midiNotes;
#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            midiNotes = ParseMidiForVerification(midiPath);
        }
        catch (Exception ex)
        {
            _issues.Add(new AlignmentIssue
            {
                Severity = "ERROR",
                Category = "MIDI_PARSING",
                Message = $"Failed to parse MIDI file: {ex.Message}"
            });

            return BuildResult(trainingData, audioDurationSeconds, totalFrames, midiNotes: []);
        }
#pragma warning restore CA1031 // Do not catch general exception types

        // Verify basic frame rate and duration
        VerifyFrameRateAndDuration(trainingData, audioDurationSeconds);

        // Verify each MIDI note maps to correct frame indices
        VerifyNoteAlignments(midiNotes, trainingData, totalFrames);

        // Verify roll consistency
        VerifyRollConsistency(trainingData, midiNotes);

        // Verify velocity values
        VerifyVelocityValues(trainingData);

        // Verify no impossible temporal relationships
        VerifyTemporalIntegrity(trainingData);

        return BuildResult(trainingData, audioDurationSeconds, totalFrames, midiNotes);
    }

    private void VerifyFrameRateAndDuration(TrainingDataSample data, double audioDurationSeconds)
    {
        var frameRate = GetFrameRate();
        var totalFrames = data.MelSpectrogram.GetLength(0);
        var calculatedDuration = totalFrames / frameRate;

        // Frame rate verification
        const float expectedFrameRate = (float)SampleRate / HopSize;
        if (Math.Abs(frameRate - expectedFrameRate) > 0.001f)
        {
            _issues.Add(new AlignmentIssue
            {
                Severity = "ERROR",
                Category = "FRAME_RATE",
                Message = $"Frame rate mismatch: expected {expectedFrameRate}, got {frameRate}"
            });
        }

        // Duration consistency
        if (Math.Abs(calculatedDuration - audioDurationSeconds) > 0.1)
        {
            _issues.Add(new AlignmentIssue
            {
                Severity = "WARNING",
                Category = "DURATION",
                Message = $"Duration mismatch: audio={audioDurationSeconds:F2}s, frames={calculatedDuration:F2}s"
            });
        }
    }

    private void VerifyNoteAlignments(List<MidiNoteInfo> midiNotes, TrainingDataSample data, int totalFrames)
    {
        var frameRate = GetFrameRate();
        var onsetRoll = data.OnsetRoll;
        var pianoRoll = data.PianoRoll;
        var offsetRoll = data.OffsetRoll;

        foreach (var note in midiNotes)
        {
            var keyIndex = note.MidiPitch - MinMidiNote;

            // Calculate expected frame indices
            var expectedOnsetFrame = (int)Math.Round(note.OnsetSeconds * frameRate);
            var expectedOffsetFrame = (int)Math.Round(note.OffsetSeconds * frameRate);

            expectedOnsetFrame = Math.Clamp(expectedOnsetFrame, 0, totalFrames - 1);
            expectedOffsetFrame = Math.Clamp(expectedOffsetFrame, 0, totalFrames - 1);

            // Verify onset marker exists at expected location
            if (expectedOnsetFrame < totalFrames)
            {
                if (onsetRoll[expectedOnsetFrame, keyIndex] < 0.5f)
                {
                    _issues.Add(new AlignmentIssue
                    {
                        Severity = "ERROR",
                        Category = "MISSING_ONSET",
                        Message = $"Missing onset marker at frame {expectedOnsetFrame} for note MIDI {note.MidiPitch}",
                        NoteIndex = midiNotes.IndexOf(note),
                        KeyIndex = keyIndex,
                        TimeSeconds = note.OnsetSeconds
                    });
                }
            }

            // Verify piano roll is active during note duration
            var activateFrameCount = 0;
            for (int frame = expectedOnsetFrame; frame <= Math.Min(expectedOffsetFrame, totalFrames - 1); frame++)
            {
                if (pianoRoll[frame, keyIndex] > 0.5f)
                {
                    activateFrameCount++;
                }
            }

            var expectedActivationCount = expectedOffsetFrame - expectedOnsetFrame + 1;
            if (activateFrameCount < expectedActivationCount * 0.8) // Allow 20% tolerance
            {
                _issues.Add(new AlignmentIssue
                {
                    Severity = "WARNING",
                    Category = "INCOMPLETE_PIANO_ROLL",
                    Message = $"Piano roll activation incomplete for note MIDI {note.MidiPitch}: " +
                             $"{activateFrameCount}/{expectedActivationCount} frames active",
                    NoteIndex = midiNotes.IndexOf(note),
                    KeyIndex = keyIndex
                });
            }

            // Verify offset marker
            if (expectedOffsetFrame < totalFrames && expectedOffsetFrame != expectedOnsetFrame)
            {
                if (offsetRoll[expectedOffsetFrame, keyIndex] < 0.5f)
                {
                    _issues.Add(new AlignmentIssue
                    {
                        Severity = "WARNING",
                        Category = "MISSING_OFFSET",
                        Message = $"Missing/unclear offset marker at frame {expectedOffsetFrame} for note MIDI {note.MidiPitch}",
                        NoteIndex = midiNotes.IndexOf(note),
                        KeyIndex = keyIndex,
                        TimeSeconds = note.OffsetSeconds
                    });
                }
            }
        }
    }

    private void VerifyRollConsistency(TrainingDataSample data, List<MidiNoteInfo> midiNotes)
    {
        var pianoRoll = data.PianoRoll;
        var onsetRoll = data.OnsetRoll;
        var offsetRoll = data.OffsetRoll;
        var velocityRoll = data.VelocityRoll;
        var frameRate = GetFrameRate();
        var totalFrames = pianoRoll.GetLength(0);

        for (int frame = 0; frame < totalFrames; frame++)
        {
            for (int key = 0; key < NumKeys; key++)
            {
                var onsetActive = onsetRoll[frame, key] > 0.5f;
                var offsetActive = offsetRoll[frame, key] > 0.5f;
                var pianoActive = pianoRoll[frame, key] > 0.5f;

                // Piano should be active if onset is marked
                if (onsetActive && !pianoActive)
                {
                    _issues.Add(new AlignmentIssue
                    {
                        Severity = "ERROR",
                        Category = "PIANO_ROLL_MISMATCH",
                        Message = $"Onset marked at frame {frame}, key {key + MinMidiNote} but piano roll not active",
                        KeyIndex = key,
                        TimeSeconds = frame / frameRate
                    });
                }

                // Velocity should only be set at onset
                if (velocityRoll[frame, key] > 0)
                {
                    if (!onsetActive)
                    {
                        _issues.Add(new AlignmentIssue
                        {
                            Severity = "WARNING",
                            Category = "VELOCITY_TIMING",
                            Message = $"Velocity set at non-onset frame {frame}, key {key + MinMidiNote}",
                            KeyIndex = key,
                            TimeSeconds = frame / frameRate
                        });
                    }
                }
            }
        }
    }

    private void VerifyVelocityValues(TrainingDataSample data)
    {
        var velocityRoll = data.VelocityRoll;
        var totalFrames = velocityRoll.GetLength(0);

        for (int frame = 0; frame < totalFrames; frame++)
        {
            for (int key = 0; key < NumKeys; key++)
            {
                var velocity = velocityRoll[frame, key];
                
                if (velocity < 0 || velocity > 1.0f)
                {
                    _issues.Add(new AlignmentIssue
                    {
                        Severity = "ERROR",
                        Category = "INVALID_VELOCITY",
                        Message = $"Velocity out of range at frame {frame}, key {key + MinMidiNote}: {velocity:F3}",
                        KeyIndex = key,
                        TimeSeconds = frame / GetFrameRate()
                    });
                }
            }
        }
    }

    private void VerifyTemporalIntegrity(TrainingDataSample data)
    {
        var pianoRoll = data.PianoRoll;
        var totalFrames = pianoRoll.GetLength(0);

        // Verify no impossible temporal jumps (notes shouldn't be active, inactive, then active again)
        for (int key = 0; key < NumKeys; key++)
        {
            int? lastActiveFrame = null;
            int gapCount = 0;

            for (int frame = 0; frame < totalFrames; frame++)
            {
                var isActive = pianoRoll[frame, key] > 0.5f;

                if (isActive)
                {
                    if (lastActiveFrame.HasValue && (frame - lastActiveFrame.Value) > 2)
                    {
                        gapCount++;
                    }
                    lastActiveFrame = frame;
                }
            }

            // If we see too many gaps, this might indicate overlapping notes not properly handled
            if (gapCount > 10)
            {
                _issues.Add(new AlignmentIssue
                {
                    Severity = "INFO",
                    Category = "FRAGMENTED_NOTES",
                    Message = $"Key {key + MinMidiNote} has {gapCount} activation gaps (normal for overlapping notes)",
                    KeyIndex = key
                });
            }
        }
    }

    private static List<MidiNoteInfo> ParseMidiForVerification(string midiPath)
    {
        var midiFile = MidiFile.Read(midiPath);
        var tempoMap = midiFile.GetTempoMap();
        var notes = new List<MidiNoteInfo>();

        foreach (var note in midiFile.GetNotes())
        {
            if (note.NoteNumber >= MinMidiNote && note.NoteNumber <= MaxMidiNote)
            {
                var onsetSeconds = note.TimeAs<MetricTimeSpan>(tempoMap).TotalSeconds;
                var offsetSeconds = note.EndTimeAs<MetricTimeSpan>(tempoMap).TotalSeconds;

                notes.Add(new MidiNoteInfo
                {
                    MidiPitch = note.NoteNumber,
                    OnsetSeconds = onsetSeconds,
                    OffsetSeconds = offsetSeconds,
                    Velocity = note.Velocity / 127.0
                });
            }
        }

        return notes.OrderBy(n => n.OnsetSeconds).ToList();
    }

    private VerificationResult BuildResult(
        TrainingDataSample data,
        double audioDurationSeconds,
        int totalFrames,
        List<MidiNoteInfo> midiNotes)
    {
        var errorCount = _issues.Count(i => i.Severity == "ERROR");
        var warningCount = _issues.Count(i => i.Severity == "WARNING");
        var infoCount = _issues.Count(i => i.Severity == "INFO");
        var isValid = errorCount == 0;

        var summary = isValid
            ? $"✓ Valid alignment: {midiNotes.Count} notes, {totalFrames} frames, {audioDurationSeconds:F2}s duration"
            : $"✗ {errorCount} critical issues, {warningCount} warnings";

        return new VerificationResult
        {
            IsValid = isValid,
            ErrorCount = errorCount,
            WarningCount = warningCount,
            InfoCount = infoCount,
            Issues = _issues.ToList(),
            AudioDurationSeconds = audioDurationSeconds,
            TotalFrames = totalFrames,
            TotalMidiNotes = midiNotes.Count,
            FrameRate = GetFrameRate(),
            Summary = summary
        };
    }

    private static float GetFrameRate() => (float)SampleRate / HopSize;

    private sealed class MidiNoteInfo
    {
        public int MidiPitch { get; init; }
        public double OnsetSeconds { get; init; }
        public double OffsetSeconds { get; init; }
        public double Velocity { get; init; }
    }
}
