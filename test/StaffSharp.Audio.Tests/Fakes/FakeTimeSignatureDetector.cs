using StaffSharp.Audio.Analysis.Meter;
using StaffSharp.Performance;

namespace StaffSharp.Audio.Tests.Fakes;

internal sealed class FakeTimeSignatureDetector : ITimeSignatureDetector
{
    private readonly IReadOnlyList<TimeSignatureChange> _timeSignatures;

    public FakeTimeSignatureDetector(IReadOnlyList<TimeSignatureChange> timeSignatures)
    {
        _timeSignatures = timeSignatures;
    }

    public IReadOnlyList<TimeSignatureChange>? DetectTimeSignatures(ReadOnlySpan<double> onsets, double? estimatedTempo = null) =>
        _timeSignatures;
}
