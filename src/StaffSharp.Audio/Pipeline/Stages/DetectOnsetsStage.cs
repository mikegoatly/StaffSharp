using StaffSharp.Audio.Analysis.Boundaries;
using StaffSharp.Audio.Analysis.Onset;

namespace StaffSharp.Audio.Pipeline.Stages;

/// <summary>
/// Pipeline stage that detects onset times (note attacks) in audio.
/// </summary>
internal sealed class DetectOnsetsStage : PipelineStageBase
{
    private readonly IOnsetDetector _detector;
    protected override string StageName => "DetectOnsets";

    public DetectOnsetsStage(AudioPipelineOptions options, IOnsetDetector detector) : base(options)
    {
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
    }

    /// <summary>
    /// Detects onset times from the audio content region.
    /// </summary>
    /// <param name="audio">The audio buffer to analyze.</param>
    /// <param name="boundaries">The content boundaries (non-silent region).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Array of onset times in seconds.</returns>
    public Task<double[]> ExecuteAsync(
        AudioBuffer audio,
        AudioBoundaries boundaries,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        ReportProgress("Detecting onsets");

        // Extract the content region (excluding leading/trailing silence)
        var slice = audio.Samples.Span.Slice(
            boundaries.StartSample,
            boundaries.EndSample - boundaries.StartSample);

        var onsets = _detector.DetectOnsets(
            slice,
            audio.SampleRate,
            boundaries.StartTime);

        EmitDiagnostics("Detected onset count", onsets.Length);
        EmitDiagnostics("Onsets", onsets);

        return Task.FromResult(onsets);
    }
}
