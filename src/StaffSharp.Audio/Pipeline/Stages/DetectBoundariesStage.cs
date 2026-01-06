using StaffSharp.Audio.Analysis.Boundaries;

namespace StaffSharp.Audio.Pipeline.Stages;

/// <summary>
/// Pipeline stage that detects audio boundaries (leading/trailing silence).
/// </summary>
internal sealed class DetectBoundariesStage : PipelineStageBase
{
    private readonly IAudioBoundaryDetector _detector;
    protected override string StageName => "DetectBoundaries";

    public DetectBoundariesStage(AudioPipelineOptions options, IAudioBoundaryDetector detector) : base(options)
    {
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
    }

    /// <summary>
    /// Detects the content boundaries in the audio, excluding leading/trailing silence.
    /// </summary>
    /// <param name="audio">The audio buffer to analyze.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The detected audio boundaries.</returns>
    public Task<AudioBoundaries> ExecuteAsync(AudioBuffer audio, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        ReportProgress("Detecting audio boundaries");

        var boundaries = _detector.DetectBoundaries(audio);

        if (boundaries == null)
        {
            throw new InvalidOperationException("Boundary detection failed - detector returned null.");
        }

        EmitDiagnostics("Leading silence", boundaries.LeadingSilence);
        EmitDiagnostics("Trailing silence", boundaries.TrailingSilence);
        EmitDiagnostics("Start sample", boundaries.StartSample);
        EmitDiagnostics("End sample", boundaries.EndSample);
        EmitDiagnostics("Content duration", boundaries.ContentDuration);


        return Task.FromResult(boundaries);
    }
}
