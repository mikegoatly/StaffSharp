using StaffSharp.Audio.Analysis;
using StaffSharp.Audio.Analysis.Meter;
using StaffSharp;
using StaffSharp.Notation;
using StaffSharp.Performance;

namespace StaffSharp.Audio.Pipeline.Stages;

/// <summary>
/// Pipeline stage that detects time signatures from onset patterns.
/// </summary>
internal sealed class DetectTimeSignatureStage : IAsyncPipelineStage<double[], IReadOnlyList<TimeSignatureChange>>
{
    private readonly ITimeSignatureDetector? _detector;

    public string StageName => "DetectTimeSignature";

    public DetectTimeSignatureStage(ITimeSignatureDetector? detector = null)
    {
        _detector = detector;
    }

    public Task<IReadOnlyList<TimeSignatureChange>> ProcessAsync(double[] input, AudioPipelineContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<TimeSignatureChange> timeSignatures;

        if (_detector == null)
        {
            // No detector configured - use default 4/4 time signature
            timeSignatures = new[]
            {
                new TimeSignatureChange(
                    Rational.Zero,
                    TimeSignature.CommonTime
                )
            };

            context.EmitDiagnostics(StageName, "DetectorUsed", "Default (4/4)");
        }
        else
        {
            var detected = _detector.DetectTimeSignatures(input);

            if (detected == null || detected.Count == 0)
            {
                // Detection failed - use default 4/4
                timeSignatures = new[]
                {
                    new TimeSignatureChange(
                        Rational.Zero,
                        TimeSignature.CommonTime
                    )
                };

                context.EmitDiagnostics(StageName, "DetectorUsed", "Failed - Fallback to 4/4");
            }
            else
            {
                timeSignatures = detected;
                context.EmitDiagnostics(StageName, "DetectorUsed", _detector.GetType().Name);
            }
        }

        context.EmitDiagnostics(StageName, "TimeSignatureCount", timeSignatures.Count);
        context.EmitDiagnostics(StageName, "TimeSignatures", () => timeSignatures);

        context.TimeSignatures = timeSignatures;
        return Task.FromResult(timeSignatures);
    }
}
