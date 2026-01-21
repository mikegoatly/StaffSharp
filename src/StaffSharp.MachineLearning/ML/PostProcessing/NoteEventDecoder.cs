namespace StaffSharp.MachineLearning.ML.PostProcessing;

using System;
using System.Collections.Generic;

using StaffSharp.MachineLearning.ML.Models;
using StaffSharp.MachineLearning.Options;

/// <summary>
/// Decodes piano roll predictions into discrete note events.
/// IMPORTANT: This implementation MUST be kept consistent with the Python equivalent in training/scripts/decode_notes.py
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
internal sealed class NoteEventDecoder
{
    private const int PianoKeyCount = 88;
    private const int LowestPianoKey = 21;

    private readonly MLTranscriptionOptions _options;

    public NoteEventDecoder(MLTranscriptionOptions? options = null)
    {
        _options = options ?? new MLTranscriptionOptions();
    }

    public IReadOnlyList<NoteEvent> Decode(PolyphonicTranscriptionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        ValidateResult(result);

        var notes = new List<NoteEvent>();
        var frameDuration = 1.0 / result.FrameRate;

        // Calculate gap tolerance in frames from time-based setting
        int gapTolerance = (int)Math.Ceiling(_options.MinGapSeconds * result.FrameRate);

        for (int keyIndex = 0; keyIndex < PianoKeyCount; keyIndex++)
        {
            var midiNote = LowestPianoKey + keyIndex;
            DecodeKeyNotes(result, keyIndex, midiNote, frameDuration, notes, gapTolerance);
        }

        notes.Sort((a, b) => a.Onset.CompareTo(b.Onset));
        return notes;
    }

    private void DecodeKeyNotes(
        PolyphonicTranscriptionResult result,
        int keyIndex,
        int midiNote,
        double frameDuration,
        List<NoteEvent> notes,
        int gapTolerance)
    {
        var numFrames = result.NumFrames;

        // State Tracking
        int? activeNoteStartFrame = null;
        float activeNoteVelocity = 0f;
        int gapFrameCount = 0; // How many frames have we been "missing" the note?

        for (int frameIndex = 0; frameIndex < numFrames; frameIndex++)
        {
            var onsetProb = result.OnsetRoll[frameIndex, keyIndex];
            var offsetProb = result.OffsetRoll[frameIndex, keyIndex];
            var frameProb = result.PianoRoll[frameIndex, keyIndex];
            var velocity = result.VelocityRoll[frameIndex, keyIndex];

            var isOnset = onsetProb >= _options.OnsetThreshold;
            var isOffset = offsetProb >= _options.OffsetThreshold;
            var isActive = frameProb >= _options.FrameThreshold;

            // 1. Check for New Onset
            // We enforce a "Consensus" check: Frame prob shouldn't be zero.
            if (isOnset && frameProb > _options.MinFrameForOnset)
            {
                // If velocity is too low, ignore this onset entirely (Ghost busting)
                if (velocity < _options.MinVelocity)
                {
                    continue;
                }

                // If there was an active note, end it immediately (Re-articulation)
                // Note: We use (frameIndex - gapFrameCount) to trim any trailing silence if we were in a gap
                if (activeNoteStartFrame.HasValue)
                {
                    TryCreateNote(midiNote, activeNoteStartFrame.Value, frameIndex - gapFrameCount, activeNoteVelocity, frameDuration, notes);
                }

                // Start new note
                activeNoteStartFrame = frameIndex;
                activeNoteVelocity = velocity;
                gapFrameCount = 0;
            }
            // 2. Handle Active Note Logic
            else if (activeNoteStartFrame.HasValue)
            {
                // Determine if we should turn off the note
                bool explicitStop = isOffset; // Offset head says STOP
                bool signalLost = !isActive;  // Frame head says SILENCE

                if (explicitStop)
                {
                    // If Offset Head fires, we trust it and stop immediately.
                    TryCreateNote(midiNote, activeNoteStartFrame.Value, frameIndex, activeNoteVelocity, frameDuration, notes);
                    activeNoteStartFrame = null;
                    activeNoteVelocity = 0f;
                    gapFrameCount = 0;
                }
                else if (signalLost)
                {
                    // Signal died, but no explicit offset.
                    // Start counting the gap.
                    gapFrameCount++;

                    // If gap is too long, confirm the kill.
                    if (gapFrameCount > gapTolerance)
                    {
                        // The note actually ended 'gapTolerance' frames ago
                        int actualEndFrame = frameIndex - gapTolerance;

                        TryCreateNote(midiNote, activeNoteStartFrame.Value, actualEndFrame, activeNoteVelocity, frameDuration, notes);

                        activeNoteStartFrame = null;
                        activeNoteVelocity = 0f;
                        gapFrameCount = 0;
                    }
                }
                else
                {
                    // Signal is alive!
                    // Reset gap counter (Bridge the gap)
                    gapFrameCount = 0;
                }
            }
        }

        // Handle end of file
        if (activeNoteStartFrame.HasValue)
        {
            // If we ended while in a gap, trim the gap
            int finalFrame = numFrames - gapFrameCount;
            TryCreateNote(midiNote, activeNoteStartFrame.Value, finalFrame, activeNoteVelocity, frameDuration, notes);
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
        // Sanity check: Ensure positive length
        if (endFrame <= startFrame) return;

        var durationSeconds = (endFrame - startFrame) * frameDuration;

        // Filter min length
        if (durationSeconds < _options.MinNoteLengthSeconds) return;

        // Note: MinVelocity check is done at Onset detection, 
        // but can be repeated here if logic changes.

        var onset = TimeSpan.FromSeconds(startFrame * frameDuration);
        var duration = TimeSpan.FromSeconds(durationSeconds);
        var clampedVelocity = Math.Clamp(velocity, 0f, 1f);

        notes.Add(new NoteEvent(
            Pitch: MidiNote.Create(midiNote),
            Onset: onset,
            Duration: duration,
            Velocity: Velocity.Create(clampedVelocity)
        ));
    }

    private static void ValidateResult(PolyphonicTranscriptionResult result)
    {
        if (result.FrameRate <= 0)
        {
            throw new ArgumentException("Frame rate must be positive", nameof(result));
        }

        var numFrames = result.PianoRoll.GetLength(0);
        var numKeys = result.PianoRoll.GetLength(1);

        if (numKeys != PianoKeyCount)
        {
            throw new ArgumentException($"Piano roll must have {PianoKeyCount} keys (MIDI 21-108), got {numKeys}", nameof(result));
        }

        if (result.OnsetRoll.GetLength(0) != numFrames || result.OnsetRoll.GetLength(1) != PianoKeyCount)
        {
            throw new ArgumentException($"Onset roll must have {PianoKeyCount} keys and match piano roll frame count", nameof(result));
        }

        if (result.OffsetRoll.GetLength(0) != numFrames || result.OffsetRoll.GetLength(1) != PianoKeyCount)
        {
            throw new ArgumentException($"Offset roll must have {PianoKeyCount} keys and match piano roll frame count", nameof(result));
        }

        if (result.VelocityRoll.GetLength(0) != numFrames || result.VelocityRoll.GetLength(1) != PianoKeyCount)
        {
            throw new ArgumentException($"Velocity roll must have {PianoKeyCount} keys and match piano roll frame count", nameof(result));
        }
    }
}
