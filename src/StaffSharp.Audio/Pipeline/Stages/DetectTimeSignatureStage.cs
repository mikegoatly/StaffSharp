using StaffSharp.Audio.Analysis.Meter;
using StaffSharp.Notation;
using StaffSharp.Performance;

namespace StaffSharp.Audio.Pipeline.Stages;

/// <summary>
/// Pipeline stage that detects time signatures from onset patterns.
/// </summary>
internal sealed class DetectTimeSignatureStage : PipelineStageBase
{
    private readonly ITimeSignatureDetector? _detector;
    protected override string StageName => "DetectTimeSignature";

    public DetectTimeSignatureStage(AudioPipelineOptions options, ITimeSignatureDetector? detector = null) : base(options)
    {
        _detector = detector;
    }

    /// <summary>
    /// Detects time signatures from onset timing patterns.
    /// Returns 4/4 if no detector configured or detection fails.
    /// </summary>
    /// <param name="onsets">Array of onset times in seconds.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of time signature changes.</returns>
    public Task<IReadOnlyList<TimeSignatureChange>> ExecuteAsync(
        double[] onsets,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        ReportProgress("Detecting time signatures...");

        IReadOnlyList<TimeSignatureChange> timeSignatures;

        if (_detector == null)
        {
            // No detector configured - use default 4/4 time signature
            timeSignatures = new[]
            {
                new TimeSignatureChange(
                    Rational.Zero,
                    TimeSignature.CommonTime)
            };
        }
        else
        {
            var detected = _detector.DetectTimeSignatures(onsets);

            if (detected == null || detected.Count == 0)
            {
                // Detection failed - use default 4/4
                timeSignatures = new[]
                {
                    new TimeSignatureChange(
                        Rational.Zero,
                        TimeSignature.CommonTime)
                };
            }
            else
            {
                timeSignatures = detected;
            }
        }

        EmitDiagnostics("TimeSignatureCount", timeSignatures.Count);

        return Task.FromResult(timeSignatures);
    }
}
