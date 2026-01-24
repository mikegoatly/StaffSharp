using StaffSharp.Audio.Analysis.Boundaries;
using StaffSharp.Audio.Analysis.Meter;
using StaffSharp.Audio.Analysis.Onset;
using StaffSharp.Audio.Analysis.Pitch;
using StaffSharp.Audio.Analysis.Tempo;
using StaffSharp.Audio.Pipeline;
using StaffSharp.Audio.Pipeline.Stages;
using StaffSharp.Performance;
using StaffSharp.Quantization;

namespace StaffSharp.Audio.Analysis;

/// <summary>
/// Algorithmic note detector that uses traditional signal processing approaches.
/// Combines onset detection, pitch detection, tempo analysis, and quantization
/// to produce musical note events from audio.
/// </summary>
public sealed class AlgorithmicNoteDetector : INoteDetector
{
    private readonly SpectralFluxOnsetDetector _onsetDetector;
    private readonly PyinPitchDetector _pitchDetector;
    private readonly SimpleTimeSignatureDetector _timeSignatureDetector;
    private readonly ITempoDetector _tempoDetector;
    private readonly MonophonicQuantizer _quantizer;
    private readonly EnergyBasedBoundaryDetector _boundaryDetector;

    /// <summary>
    /// Creates a new algorithmic note detector with the specified options.
    /// All options are nullable and will use sensible defaults if not specified.
    /// </summary>
    /// <param name="options">Configuration options. If null, uses all defaults.</param>
    public AlgorithmicNoteDetector(AlgorithmicNoteDetectorOptions? options = null)
    {
        options ??= new AlgorithmicNoteDetectorOptions();

        // Build components from options (use defaults if options not specified)
        _onsetDetector = new SpectralFluxOnsetDetector(options.OnsetOptions);
        _pitchDetector = new PyinPitchDetector(options.PitchOptions);
        _timeSignatureDetector = new SimpleTimeSignatureDetector();
        _tempoDetector = TempoDetectorFactory.Create(options.TempoOptions);
        _quantizer = new MonophonicQuantizer();
        _boundaryDetector = new EnergyBasedBoundaryDetector(options.BoundaryOptions);
    }

    /// <summary>
    /// Detects and quantizes notes from audio using algorithmic signal processing.
    /// </summary>
    /// <param name="audio">Normalized audio buffer to transcribe.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Quantization result containing quantized notes and tempo map.</returns>
    public async Task<PerformanceTimeline> DetectAsync(PipelineProgress progress, AudioBuffer audio, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(progress);
        ArgumentNullException.ThrowIfNull(audio);
        ct.ThrowIfCancellationRequested();

        if (audio.Channels > 1)
        {
            progress.ReportProgress("Normalizing audio to mono");
            audio = audio.ToMono();
        }

        progress.ReportProgress("Normalizing audio");
        (audio, var normalizationReport) = audio.Normalize();
        progress.EmitDiagnostics("Normalization report", normalizationReport);

        progress.EmitDiagnostics("Channels", audio.Channels);
        progress.EmitDiagnostics("SampleCount", audio.SampleCount);

        // Detect boundaries (trim silence)
        var boundaries = _boundaryDetector.DetectBoundaries(progress, audio)
            ?? throw new InvalidOperationException("Boundary detection failed: detector returned null. This can happen if the input audio is silent, extremely short, or incompatible with the configured boundary detector. Verify the audio buffer contains usable signal and that the boundary detector is correctly configured.");

        // Detect onsets
        // Extract the content region (excluding leading/trailing silence)
        var slice = audio.Samples.Span[boundaries.StartSample..boundaries.EndSample];

        // Detect onsets in the content region
        // Note: We pass TimeSpan.Zero as the offset because we want onset times
        // relative to the content start (beat 0), not the original audio file start.
        // The boundary detection has already trimmed the leading silence.
        var onsets = _onsetDetector.DetectOnsets(progress, slice, audio.SampleRate, TimeSpan.Zero);

        // Detect pitches and time signatures in parallel
        var pitchTask = new DetectPitchesStage(progress, _pitchDetector)
            .ExecuteAsync(onsets, audio, boundaries, ct);
        var timeSigTask = Task.Run(() => _timeSignatureDetector.DetectTimeSignatures(
                progress with { StageName = "Detect time signature" },
                onsets));

        var pitches = await pitchTask.ConfigureAwait(false);
        var timeSignatures = await timeSigTask.ConfigureAwait(false);

        // Filter unpitched onsets
        var (filteredOnsets, filteredPitches) = await new FilterUnpitchedOnsetsStage(progress with { StageName = "Filter pitches" })
            .ExecuteAsync(onsets, pitches, ct).ConfigureAwait(false);

        // Detect tempo
        var tempoChanges = _tempoDetector.DetectTempo(
            progress with { StageName = "Detect tempo" },
            filteredOnsets);

        // Combine detected tempo and time signatures into a single TempoMap
        var tempoMap = new TempoMap(tempoChanges, timeSignatures);

        // Quantize (infers durations from onset spacing)
        var (quantizedNotes, refinedTempoMap) = _quantizer.Quantize(
            filteredOnsets,
            filteredPitches,
            tempoMap);

        return new PerformanceTimeline(refinedTempoMap, quantizedNotes);
    }

    public void Dispose()
    {
    }
}
