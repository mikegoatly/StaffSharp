namespace StaffSharp.MachineLearning.ML.PostProcessing;

using System;
using System.Collections.Generic;

using StaffSharp.MachineLearning.ML.Models;
using StaffSharp.MachineLearning.Options;

/// <summary>
/// Decodes piano roll predictions into discrete note events.
/// </summary>
/// <remarks>
/// Converts continuous frame-level predictions (onset probabilities, frame activations, velocities)
/// into discrete note events with onset time, duration, pitch, and velocity.
///
/// Algorithm:
/// 1. For each of 88 piano keys (MIDI 21-108):
/// 2.   Detect onsets where onset probability exceeds threshold
/// 3.   Track frame activations to determine note duration
/// 4.   End note when frame deactivates or new onset detected
/// 5. Filter notes shorter than minimum duration
/// 6. Sort by onset time
/// </remarks>
public sealed class NoteEventDecoder
{
    private const int PianoKeyCount = 88;
    private const int LowestPianoKey = 21; // MIDI note A0

    private readonly PolyphonicTranscriptionOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="NoteEventDecoder"/> class.
    /// </summary>
    /// <param name="options">Transcription options including thresholds.</param>
    public NoteEventDecoder(PolyphonicTranscriptionOptions? options = null)
    {
        _options = options ?? new PolyphonicTranscriptionOptions();
    }

    /// <summary>
    /// Decodes a polyphonic transcription result into a list of note events.
    /// </summary>
    /// <param name="result">The transcription result containing piano roll predictions.</param>
    /// <returns>A sorted list of note events ordered by onset time.</returns>
    public IReadOnlyList<NoteEvent> Decode(PolyphonicTranscriptionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        ValidateResult(result);

        var notes = new List<NoteEvent>();
        var frameDuration = 1.0 / result.FrameRate; // Duration of each frame in seconds

        // Process each piano key independently
        for (int keyIndex = 0; keyIndex < PianoKeyCount; keyIndex++)
        {
            var midiNote = LowestPianoKey + keyIndex;
            DecodeKeyNotes(result, keyIndex, midiNote, frameDuration, notes);
        }

        // Sort by onset time (stable sort to preserve insertion order for simultaneous notes)
        notes.Sort((a, b) => a.Onset.CompareTo(b.Onset));

        return notes;
    }

    private void DecodeKeyNotes(
        PolyphonicTranscriptionResult result,
        int keyIndex,
        int midiNote,
        double frameDuration,
        List<NoteEvent> notes)
    {
        var numFrames = result.NumFrames;

        // Track active note state
        int? activeNoteStartFrame = null;
        float activeNoteVelocity = 0f;

        for (int frameIndex = 0; frameIndex < numFrames; frameIndex++)
        {
            var onsetProb = result.OnsetRoll[frameIndex, keyIndex];
            var offsetProb = result.OffsetRoll[frameIndex, keyIndex];
            var frameProb = result.PianoRoll[frameIndex, keyIndex];
            var velocity = result.VelocityRoll[frameIndex, keyIndex];

            var isOnset = onsetProb >= _options.OnsetThreshold;
            var isOffset = offsetProb >= _options.OffsetThreshold;
            var isActive = frameProb >= _options.FrameThreshold;

            // Case 1: New onset detected
            if (isOnset)
            {
                // If there's an active note, end it first (re-articulation)
                if (activeNoteStartFrame.HasValue)
                {
                    TryCreateNote(
                        midiNote,
                        activeNoteStartFrame.Value,
                        frameIndex,
                        activeNoteVelocity,
                        frameDuration,
                        notes);
                }

                // Start new note
                activeNoteStartFrame = frameIndex;
                activeNoteVelocity = velocity;
            }
            // Case 2: Explicit offset detected or active note becomes inactive
            else if (activeNoteStartFrame.HasValue && (isOffset || !isActive))
            {
                TryCreateNote(
                    midiNote,
                    activeNoteStartFrame.Value,
                    frameIndex,
                    activeNoteVelocity,
                    frameDuration,
                    notes);

                activeNoteStartFrame = null;
                activeNoteVelocity = 0f;
            }
        }

        // Handle note still active at end of audio
        if (activeNoteStartFrame.HasValue)
        {
            TryCreateNote(
                midiNote,
                activeNoteStartFrame.Value,
                numFrames,
                activeNoteVelocity,
                frameDuration,
                notes);
        }
    }

    private void TryCreateNote(
        int midiNote,
        int startFrame,
        int endFrame,
        float velocity,
        double frameDuration,
        List<NoteEvent> notes)
    {
        var durationSeconds = (endFrame - startFrame) * frameDuration;

        // Filter out notes shorter than minimum duration
        if (durationSeconds < _options.MinNoteLengthSeconds)
        {
            return;
        }

        // Filter out notes with zero velocity (likely false positives)
        if (velocity <= 0f)
        {
            return;
        }

        var onset = TimeSpan.FromSeconds(startFrame * frameDuration);
        var duration = TimeSpan.FromSeconds(durationSeconds);

        // Clamp velocity to valid range [0, 1]
        var clampedVelocity = Math.Clamp(velocity, 0f, 1f);

        var noteEvent = new NoteEvent(
            Pitch: MidiNote.Create(midiNote),
            Onset: onset,
            Duration: duration,
            Velocity: Velocity.Create(clampedVelocity)
        );

        notes.Add(noteEvent);
    }

    private static void ValidateResult(PolyphonicTranscriptionResult result)
    {
        if (result.PianoRoll.GetLength(1) != PianoKeyCount)
        {
            throw new ArgumentException(
                $"Piano roll must have {PianoKeyCount} keys, got {result.PianoRoll.GetLength(1)}",
                nameof(result));
        }

        if (result.OnsetRoll.GetLength(1) != PianoKeyCount)
        {
            throw new ArgumentException(
                $"Onset roll must have {PianoKeyCount} keys, got {result.OnsetRoll.GetLength(1)}",
                nameof(result));
        }

        if (result.OffsetRoll.GetLength(1) != PianoKeyCount)
        {
            throw new ArgumentException(
                $"Offset roll must have {PianoKeyCount} keys, got {result.OffsetRoll.GetLength(1)}",
                nameof(result));
        }

        if (result.VelocityRoll.GetLength(1) != PianoKeyCount)
        {
            throw new ArgumentException(
                $"Velocity roll must have {PianoKeyCount} keys, got {result.VelocityRoll.GetLength(1)}",
                nameof(result));
        }

        if (result.FrameRate <= 0)
        {
            throw new ArgumentException("Frame rate must be positive", nameof(result));
        }
    }
}
