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
/// <remarks>
/// Creates a new algorithmic note detector with the specified components.
/// </remarks>
/// <param name="onsetDetector">Detector for finding note attack times.</param>
/// <param name="pitchDetector">Detector for identifying note pitches.</param>
/// <param name="timeSignatureDetector">Detector for identifying time signatures.</param>
/// <param name="tempoDetector">Detector for identifying tempo.</param>
/// <param name="quantizer">Quantizer for snapping notes to rhythmic grid.</param>
/// <param name="boundaryDetector">Optional boundary detector (defaults to energy-based).</param>
/// <param name="options">Optional pipeline options for progress/diagnostics.</param>
public sealed class AlgorithmicNoteDetector(
    IOnsetDetector onsetDetector,
    IPitchDetector pitchDetector,
    ITimeSignatureDetector timeSignatureDetector,
    ITempoDetector tempoDetector,
    IMonophonicQuantizer quantizer,
    IAudioBoundaryDetector boundaryDetector) : INoteDetector
{
    private readonly IAudioBoundaryDetector _boundaryDetector = boundaryDetector;

    /// <summary>
    /// Detects and quantizes notes from audio using algorithmic signal processing.
    /// </summary>
    /// <param name="audio">Normalized audio buffer to transcribe.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Quantization result containing quantized notes and tempo map.</returns>
    public async Task<PerformanceTimeline> DetectAsync(AudioPipelineOptions options, AudioBuffer audio, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(audio);
        ct.ThrowIfCancellationRequested();

        var progress = PipelineProgress.ForPipeline(options);

        // Step 1: Detect boundaries (trim silence)
        var boundaries = _boundaryDetector.DetectBoundaries(progress, audio)
            ?? throw new InvalidOperationException("Boundary detection failed: detector returned null. This can happen if the input audio is silent, extremely short, or incompatible with the configured boundary detector. Verify the audio buffer contains usable signal and that the boundary detector is correctly configured.");


        // Step 2: Detect onsets
        // Extract the content region (excluding leading/trailing silence)
        var slice = audio.Samples.Span[boundaries.StartSample..boundaries.EndSample];

        // Detect onsets in the content region
        // Note: We pass TimeSpan.Zero as the offset because we want onset times
        // relative to the content start (beat 0), not the original audio file start.
        // The boundary detection has already trimmed the leading silence.
        var onsets = onsetDetector.DetectOnsets(progress, slice, audio.SampleRate, TimeSpan.Zero);

        // Step 3: Detect pitches and time signatures in parallel
        var pitchTask = new DetectPitchesStage(progress, pitchDetector)
            .ExecuteAsync(onsets, audio, boundaries, ct);
        var timeSigTask = Task.Run(() => timeSignatureDetector.DetectTimeSignatures(
                progress with { StageName = "Detect time signature" },
                onsets));

        var pitches = await pitchTask.ConfigureAwait(false);
        var timeSignatures = await timeSigTask.ConfigureAwait(false);

        // Step 4: Filter unpitched onsets
        var (filteredOnsets, filteredPitches) = await new FilterUnpitchedOnsetsStage(progress with { StageName = "Filter pitches" })
            .ExecuteAsync(onsets, pitches, ct).ConfigureAwait(false);

        // Step 5: Detect tempo
        var tempoMap = tempoDetector.DetectTempo(progress with { StageName = "Detect tempo" }, filteredOnsets);

        // Step 6: Quantize (infers durations from onset spacing)
        var (quantizedNotes, refinedTempoMap) = quantizer.Quantize(
            filteredOnsets,
            filteredPitches,
            timeSignatures,
            tempoMap);

        return new PerformanceTimeline(refinedTempoMap, quantizedNotes);
    }
}
