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
public sealed class PolyphonicNoteDetector(
    IPolyphonicTranscriber transcriber,
    ITimeSignatureDetector timeSignatureDetector,
    ITempoDetector tempoDetector,
    IPolyphonicQuantizer quantizer,
    PolyphonicTranscriptionOptions? transcriptionOptions = null) : INoteDetector
{
    private readonly NoteEventDecoder _decoder = new NoteEventDecoder(transcriptionOptions ?? new PolyphonicTranscriptionOptions());

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
        var transcriptionResult = await transcriber.TranscribeAsync(audio).ConfigureAwait(false);

        progress.EmitDiagnostics("Frame count", transcriptionResult.NumFrames);
        progress.EmitDiagnostics("Duration (seconds)", transcriptionResult.DurationSeconds);
        progress.EmitDiagnostics("Frame rate (Hz)", transcriptionResult.FrameRate);

        progress.ReportProgress("Decoding note events from predictions");

        // Step 2: Decode piano roll to note events
        var noteEvents = _decoder.Decode(transcriptionResult);

        progress.EmitDiagnostics("Note count", noteEvents.Count);

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
        var estimatedTempo = tempoDetector.DetectTempo(progress, onsetTimes);

        // Validate detectors returned non-null results
        if (timeSignatures == null || timeSignatures.Count == 0)
        {
            timeSignatures = [new TimeSignatureChange(Rational.Zero, Notation.TimeSignature.CommonTime)];
        }

        if (estimatedTempo == null || estimatedTempo.TempoChanges.Count == 0)
        {
            throw new InvalidOperationException("Tempo detection failed - detector returned null or empty tempo map.");
        }

        var detectedTempo = estimatedTempo.TempoChanges[0].BeatsPerMinute;
        progress.EmitDiagnostics("Estimated tempo (BPM)", detectedTempo);
        progress.EmitDiagnostics("Time signatures", timeSignatures.Count);

        progress.ReportProgress("Quantizing note events");

        // Step 5: Quantize note events (snap to rhythmic grid)
        var (quantizedNotes, refinedTempoMap) = quantizer.Quantize(
            noteEvents,
            timeSignatures,
            estimatedTempo);

        progress.EmitDiagnostics("Quantized note count", quantizedNotes.Count);

        return new PerformanceTimeline(refinedTempoMap, quantizedNotes);
    }

    /// <summary>
    /// Disposes the underlying transcriber if it implements IDisposable.
    /// </summary>
    public void Dispose()
    {
        transcriber.Dispose();
    }
}
