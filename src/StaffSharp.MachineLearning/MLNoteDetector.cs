using StaffSharp.Audio;
using StaffSharp.Audio.Analysis.Meter;
using StaffSharp.Audio.Analysis.Tempo;
using StaffSharp.Audio.Pipeline;
using StaffSharp.MachineLearning.ML.Models;
using StaffSharp.MachineLearning.ML.PostProcessing;
using StaffSharp.MachineLearning.Options;
using StaffSharp.Performance;
using StaffSharp.Quantization;

namespace StaffSharp.MachineLearning;

/// <summary>
/// ML-based polyphonic note detector using trained neural network models.
/// Transcribes polyphonic audio (e.g., piano) to note events with onsets, offsets, pitches, and velocities.
/// </summary>
/// <remarks>
/// Creates a new polyphonic note detector with the specified components.
/// </remarks>
/// <param name="transcriber">ML model for transcribing audio to piano roll.</param>
/// <param name="timeSignatureDetector">Detector for identifying time signatures.</param>
/// <param name="tempoDetector">Detector for identifying tempo.</param>
/// <param name="quantizer">Quantizer for snapping notes to rhythmic grid.</param>
/// <param name="transcriptionOptions">Optional transcription settings (thresholds, etc.).</param>
/// <param name="options">Optional pipeline options for progress/diagnostics.</param>
public sealed class MLNoteDetector(
    IMLTranscriber transcriber,
    ITimeSignatureDetector timeSignatureDetector,
    ITempoDetector tempoDetector,
    IPolyphonicQuantizer quantizer,
    MLTranscriptionOptions? transcriptionOptions = null) : INoteDetector
{
    private readonly NoteEventDecoder _decoder = new(transcriptionOptions ?? new MLTranscriptionOptions());

    public static MLNoteDetector Create(MLTranscriptionOptions? options = null)
    {
        options ??= new MLTranscriptionOptions();

#pragma warning disable CA2000 // Dispose objects before losing scope - Disposed by MLNoteDetector
        return new MLNoteDetector(
            new OnnxTranscriber(options),
            new SimpleTimeSignatureDetector(),
            new InterOnsetIntervalTempoDetector(),
            new SimplePolyphonicQuantizer(),
            options);
#pragma warning restore CA2000 // Dispose objects before losing scope
    }

    /// <summary>
    /// Detects and quantizes notes from audio using ML-based polyphonic transcription.
    /// </summary>
    /// <param name="audio">Normalized audio buffer to transcribe.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Quantization result containing quantized notes and tempo map.</returns>
    public async Task<PerformanceTimeline> DetectAsync(PipelineProgress progress, AudioBuffer audio, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(progress);
        ArgumentNullException.ThrowIfNull(audio);
        ct.ThrowIfCancellationRequested();

        progress.ReportProgress("Transcribing audio with ML model");

        // Step 1: ONNX inference (audio → piano roll)
        // Note: We pass the FULL audio to the ML model to preserve context
        var transcriptionResult = await transcriber.TranscribeAsync(progress, audio).ConfigureAwait(false);

        progress.EmitDiagnostics("Frame count", transcriptionResult.NumFrames);
        progress.EmitDiagnostics("Duration (seconds)", transcriptionResult.DurationSeconds);
        progress.EmitDiagnostics("Frame rate (Hz)", transcriptionResult.FrameRate);

        progress.ReportProgress("Decoding note events from predictions");

        // Step 2: Decode piano roll to note events
        var noteEvents = _decoder.Decode(transcriptionResult);

        // Step 2a: Shift note events to remove leading silence
        // (Align first note to beat 0, like the monophonic detector does)
        if (noteEvents.Count > 0)
        {
            var firstNoteOnset = noteEvents[0].Onset;
            if (firstNoteOnset > TimeSpan.Zero)
            {
                noteEvents = ShiftNoteEvents(noteEvents, -firstNoteOnset);
                progress.EmitDiagnostics("Leading silence trimmed", firstNoteOnset);
            }
        }

        progress.EmitDiagnostics("Note count", noteEvents.Count);
        progress.EmitDiagnostics("DecodedNoteEvents", noteEvents);

        if (noteEvents.Count == 0)
        {
            // No notes detected - return empty result with default tempo
            var defaultTempoMap = new TempoMap(
                [new TempoChange(Rational.Zero, 120.0)],
                [new TimeSignatureChange(Rational.Zero, Notation.TimeSignature.CommonTime)]
            );

            return new PerformanceTimeline(defaultTempoMap, []);
        }

        progress.ReportProgress("Analyzing tempo and time signature");

        // Step 3: Extract onset times for tempo/time signature detection
        var onsetTimes = noteEvents.Select(n => n.Onset.TotalSeconds).ToArray();

        // Step 4: Detect time signature and tempo
        var timeSignatures = timeSignatureDetector.DetectTimeSignatures(progress, onsetTimes);

        // Validate time signatures
        if (timeSignatures == null || timeSignatures.Count == 0)
        {
            timeSignatures = [new TimeSignatureChange(Rational.Zero, Notation.TimeSignature.CommonTime)];
        }

        // Detect tempo
        var tempoChanges = tempoDetector.DetectTempo(progress, onsetTimes);

        if (tempoChanges == null || tempoChanges.Count == 0)
        {
            throw new InvalidOperationException("Tempo detection failed - detector returned null or empty tempo changes.");
        }

        var detectedTempo = tempoChanges[0].BeatsPerMinute;
        progress.EmitDiagnostics("Estimated tempo (BPM)", detectedTempo);
        progress.EmitDiagnostics("Time signatures", timeSignatures.Count);

        // Combine detected tempo and time signatures into a single TempoMap
        var finalTempoMap = new TempoMap(tempoChanges, timeSignatures);

        progress.ReportProgress("Quantizing note events");

        // Step 5: Quantize note events (snap to rhythmic grid)
        var (quantizedNotes, refinedTempoMap) = quantizer.Quantize(noteEvents, finalTempoMap);

        progress.EmitDiagnostics("Quantized note count", quantizedNotes.Count);

        return new PerformanceTimeline(refinedTempoMap, quantizedNotes);
    }

    /// <summary>
    /// Shifts all note event onsets by the specified time delta.
    /// Used to trim leading silence and align notes to beat 0.
    /// </summary>
    private static IReadOnlyList<NoteEvent> ShiftNoteEvents(IReadOnlyList<NoteEvent> events, TimeSpan delta)
    {
        if (events.Count == 0 || delta == TimeSpan.Zero)
        {
            return events;
        }

        var shifted = new List<NoteEvent>(events.Count);
        foreach (var evt in events)
        {
            var newOnset = evt.Onset + delta;

            // Skip notes that would have negative onset (shouldn't happen, but be safe)
            if (newOnset < TimeSpan.Zero)
            {
                continue;
            }

            shifted.Add(evt with { Onset = newOnset });
        }

        return shifted;
    }

    /// <summary>
    /// Disposes the underlying transcriber if it implements IDisposable.
    /// </summary>
    public void Dispose()
    {
        transcriber.Dispose();
    }
}
