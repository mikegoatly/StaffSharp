using StaffSharp.Audio.Analysis.Boundaries;
using StaffSharp.Audio.Analysis.Meter;
using StaffSharp.Audio.Analysis.Onset;
using StaffSharp.Audio.Analysis.Pitch;
using StaffSharp.Audio.Analysis.Tempo;
using StaffSharp.Audio.Pipeline;
using StaffSharp.Audio.Pipeline.Stages;
using StaffSharp.Quantization;

namespace StaffSharp.Audio.Analysis;

/// <summary>
/// Algorithmic note detector that uses traditional signal processing approaches.
/// Combines onset detection, pitch detection, tempo analysis, and quantization
/// to produce musical note events from audio.
/// </summary>
public sealed class AlgorithmicNoteDetector : INoteDetector
{
    private readonly IOnsetDetector _onsetDetector;
    private readonly IPitchDetector _pitchDetector;
    private readonly ITimeSignatureDetector _timeSignatureDetector;
    private readonly ITempoDetector _tempoDetector;
    private readonly IMonophonicQuantizer _quantizer;
    private readonly IAudioBoundaryDetector _boundaryDetector;
    private readonly AudioPipelineOptions? _options;

    /// <summary>
    /// Creates a new algorithmic note detector with the specified components.
    /// </summary>
    /// <param name="onsetDetector">Detector for finding note attack times.</param>
    /// <param name="pitchDetector">Detector for identifying note pitches.</param>
    /// <param name="timeSignatureDetector">Detector for identifying time signatures.</param>
    /// <param name="tempoDetector">Detector for identifying tempo.</param>
    /// <param name="quantizer">Quantizer for snapping notes to rhythmic grid.</param>
    /// <param name="boundaryDetector">Optional boundary detector (defaults to energy-based).</param>
    /// <param name="options">Optional pipeline options for progress/diagnostics.</param>
    public AlgorithmicNoteDetector(
        IOnsetDetector onsetDetector,
        IPitchDetector pitchDetector,
        ITimeSignatureDetector timeSignatureDetector,
        ITempoDetector tempoDetector,
        IMonophonicQuantizer quantizer,
        IAudioBoundaryDetector? boundaryDetector = null,
        AudioPipelineOptions? options = null)
    {
        _onsetDetector = onsetDetector ?? throw new ArgumentNullException(nameof(onsetDetector));
        _pitchDetector = pitchDetector ?? throw new ArgumentNullException(nameof(pitchDetector));
        _timeSignatureDetector = timeSignatureDetector ?? throw new ArgumentNullException(nameof(timeSignatureDetector));
        _tempoDetector = tempoDetector ?? throw new ArgumentNullException(nameof(tempoDetector));
        _quantizer = quantizer ?? throw new ArgumentNullException(nameof(quantizer));
        _boundaryDetector = boundaryDetector ?? new EnergyBasedBoundaryDetector();
        _options = options;
    }

    /// <summary>
    /// Detects and quantizes notes from audio using algorithmic signal processing.
    /// </summary>
    /// <param name="audio">Normalized audio buffer to transcribe.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Quantization result containing quantized notes and tempo map.</returns>
    public async Task<QuantizationResult> DetectAsync(AudioBuffer audio, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(audio);
        ct.ThrowIfCancellationRequested();

        // Step 1: Detect boundaries (trim silence)
        var boundaries = await new DetectBoundariesStage(_options ?? AudioPipelineOptions.Default, _boundaryDetector)
            .ExecuteAsync(audio, ct).ConfigureAwait(false);

        // Step 2: Detect onsets
        var onsets = await new DetectOnsetsStage(_options ?? AudioPipelineOptions.Default, _onsetDetector)
            .ExecuteAsync(audio, boundaries, ct).ConfigureAwait(false);

        // Step 3: Detect pitches and time signatures in parallel
        var pitchTask = new DetectPitchesStage(_options ?? AudioPipelineOptions.Default, _pitchDetector)
            .ExecuteAsync(onsets, audio, boundaries, ct);
        var timeSigTask = new DetectTimeSignatureStage(_options ?? AudioPipelineOptions.Default, _timeSignatureDetector)
            .ExecuteAsync(onsets, ct);

        var pitches = await pitchTask.ConfigureAwait(false);
        var timeSignatures = await timeSigTask.ConfigureAwait(false);

        // Step 4: Filter unpitched onsets
        var (filteredOnsets, filteredPitches) = await new FilterUnpitchedOnsetsStage(_options ?? AudioPipelineOptions.Default)
            .ExecuteAsync(onsets, pitches, ct).ConfigureAwait(false);

        // Step 5: Detect tempo
        var tempoMap = await new DetectTempoStage(_options ?? AudioPipelineOptions.Default, _tempoDetector)
            .ExecuteAsync(filteredOnsets, timeSignatures, ct).ConfigureAwait(false);

        // Step 6: Quantize (infers durations from onset spacing)
        var (quantizedNotes, refinedTempoMap) = _quantizer.Quantize(
            filteredOnsets,
            filteredPitches,
            timeSignatures,
            tempoMap);

        return new QuantizationResult(quantizedNotes, refinedTempoMap);
    }
}
