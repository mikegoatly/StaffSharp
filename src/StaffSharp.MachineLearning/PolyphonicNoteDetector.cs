using StaffSharp.Audio.Analysis.Meter;
using StaffSharp.Audio.Analysis.Tempo;
using StaffSharp.Audio.Pipeline;
using StaffSharp.MachineLearning.ML.Models;
using StaffSharp.MachineLearning.ML.PostProcessing;
using StaffSharp.MachineLearning.Options;
using StaffSharp.Quantization;

namespace StaffSharp.MachineLearning;

/// <summary>
/// ML-based polyphonic note detector using trained neural network models.
/// Transcribes polyphonic audio (e.g., piano) to note events with onsets, offsets, pitches, and velocities.
/// </summary>
public sealed class PolyphonicNoteDetector : INoteDetector, IDisposable
{
    private readonly IPolyphonicTranscriber _transcriber;
    private readonly NoteEventDecoder _decoder;
    private readonly ITimeSignatureDetector _timeSignatureDetector;
    private readonly ITempoDetector _tempoDetector;
    private readonly IPolyphonicQuantizer _quantizer;
    private readonly AudioPipelineOptions? _options;

    /// <summary>
    /// Creates a new polyphonic note detector with the specified components.
    /// </summary>
    /// <param name="transcriber">ML model for transcribing audio to piano roll.</param>
    /// <param name="timeSignatureDetector">Detector for identifying time signatures.</param>
    /// <param name="tempoDetector">Detector for identifying tempo.</param>
    /// <param name="quantizer">Quantizer for snapping notes to rhythmic grid.</param>
    /// <param name="transcriptionOptions">Optional transcription settings (thresholds, etc.).</param>
    /// <param name="options">Optional pipeline options for progress/diagnostics.</param>
    public PolyphonicNoteDetector(
        IPolyphonicTranscriber transcriber,
        ITimeSignatureDetector timeSignatureDetector,
        ITempoDetector tempoDetector,
        IPolyphonicQuantizer quantizer,
        PolyphonicTranscriptionOptions? transcriptionOptions = null,
        AudioPipelineOptions? options = null)
    {
        _transcriber = transcriber ?? throw new ArgumentNullException(nameof(transcriber));
        _timeSignatureDetector = timeSignatureDetector ?? throw new ArgumentNullException(nameof(timeSignatureDetector));
        _tempoDetector = tempoDetector ?? throw new ArgumentNullException(nameof(tempoDetector));
        _quantizer = quantizer ?? throw new ArgumentNullException(nameof(quantizer));
        _decoder = new NoteEventDecoder(transcriptionOptions ?? new PolyphonicTranscriptionOptions());
        _options = options;
    }

    /// <summary>
    /// Convenience constructor for creating a detector with an ONNX model file.
    /// </summary>
    /// <param name="modelPath">Path to the ONNX model file.</param>
    /// <param name="timeSignatureDetector">Detector for identifying time signatures.</param>
    /// <param name="tempoDetector">Detector for identifying tempo.</param>
    /// <param name="quantizer">Quantizer for snapping notes to rhythmic grid.</param>
    /// <param name="useGpu">Whether to use GPU acceleration (requires CUDA/DirectML).</param>
    /// <param name="transcriptionOptions">Optional transcription settings (thresholds, etc.).</param>
    /// <param name="options">Optional pipeline options for progress/diagnostics.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "Transcriber is stored in field and disposed in Dispose() method")]
    public PolyphonicNoteDetector(
        string modelPath,
        ITimeSignatureDetector timeSignatureDetector,
        ITempoDetector tempoDetector,
        IPolyphonicQuantizer quantizer,
        bool useGpu = false,
        PolyphonicTranscriptionOptions? transcriptionOptions = null,
        AudioPipelineOptions? options = null)
        : this(
            new OnnxPolyphonicTranscriber(
                modelPath,
                transcriptionOptions ?? new PolyphonicTranscriptionOptions { UseGpu = useGpu }),
            timeSignatureDetector,
            tempoDetector,
            quantizer,
            transcriptionOptions ?? new PolyphonicTranscriptionOptions { UseGpu = useGpu },
            options)
    {
    }

    /// <summary>
    /// Detects and quantizes notes from audio using ML-based polyphonic transcription.
    /// </summary>
    /// <param name="audio">Normalized audio buffer to transcribe.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Quantization result containing quantized notes and tempo map.</returns>
    public async Task<QuantizationResult> DetectAsync(AudioBuffer audio, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(audio);
        ct.ThrowIfCancellationRequested();

        _options?.Progress?.Report(new("PolyphonicDetection", "Transcribing audio with ML model"));

        // Step 1: ONNX inference (audio → piano roll)
        var transcriptionResult = await Task.Run(() => _transcriber.Transcribe(audio), ct).ConfigureAwait(false);

        _options?.DiagnosticsCollector?.Collect("PolyphonicDetection", "Frame count", transcriptionResult.NumFrames);
        _options?.DiagnosticsCollector?.Collect("PolyphonicDetection", "Duration (seconds)", transcriptionResult.DurationSeconds);
        _options?.DiagnosticsCollector?.Collect("PolyphonicDetection", "Frame rate (Hz)", transcriptionResult.FrameRate);

        _options?.Progress?.Report(new("PolyphonicDetection", "Decoding note events from predictions"));

        // Step 2: Decode piano roll to note events
        var noteEvents = _decoder.Decode(transcriptionResult);

        _options?.DiagnosticsCollector?.Collect("PolyphonicDetection", "Note count", noteEvents.Count);

        if (noteEvents.Count == 0)
        {
            // No notes detected - return empty result with default tempo
            var defaultTempoMap = new Performance.TempoMap(
                new[] { new Performance.TempoChange(Rational.Zero, 120.0) },
                new[] { new Performance.TimeSignatureChange(Rational.Zero, Notation.TimeSignature.CommonTime) }
            );

            return new QuantizationResult(Array.Empty<Performance.QuantizedNoteEvent>(), defaultTempoMap);
        }

        _options?.Progress?.Report(new("PolyphonicDetection", "Analyzing tempo and time signature"));

        // Step 3: Extract onset times for tempo/time signature detection
        var onsetTimes = noteEvents.Select(n => n.Onset.TotalSeconds).ToArray();

        // Step 4: Detect time signature and tempo
        var timeSignatures = _timeSignatureDetector.DetectTimeSignatures(onsetTimes);
        var estimatedTempo = _tempoDetector.DetectTempo(onsetTimes);

        // Validate detectors returned non-null results
        if (timeSignatures == null || timeSignatures.Count == 0)
        {
            timeSignatures = new[] { new Performance.TimeSignatureChange(Rational.Zero, Notation.TimeSignature.CommonTime) };
        }

        if (estimatedTempo == null || estimatedTempo.TempoChanges.Count == 0)
        {
            throw new InvalidOperationException("Tempo detection failed - detector returned null or empty tempo map.");
        }

        var detectedTempo = estimatedTempo.TempoChanges[0].BeatsPerMinute;
        _options?.DiagnosticsCollector?.Collect("PolyphonicDetection", "Estimated tempo (BPM)", detectedTempo);
        _options?.DiagnosticsCollector?.Collect("PolyphonicDetection", "Time signatures", timeSignatures.Count);

        _options?.Progress?.Report(new("PolyphonicDetection", "Quantizing note events"));

        // Step 5: Quantize note events (snap to rhythmic grid)
        var (quantizedNotes, refinedTempoMap) = _quantizer.Quantize(
            noteEvents,
            timeSignatures,
            estimatedTempo);

        _options?.DiagnosticsCollector?.Collect("PolyphonicDetection", "Quantized note count", quantizedNotes.Count);

        return new QuantizationResult(quantizedNotes, refinedTempoMap);
    }

    /// <summary>
    /// Disposes the underlying transcriber if it implements IDisposable.
    /// </summary>
    public void Dispose()
    {
        (_transcriber as IDisposable)?.Dispose();
    }
}
