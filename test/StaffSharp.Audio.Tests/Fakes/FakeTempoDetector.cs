using StaffSharp.Audio.Analysis.Tempo;
using StaffSharp.Performance;

namespace StaffSharp.Audio.Tests.Fakes;

internal sealed class FakeTempoDetector : ITempoDetector
{
    private readonly TempoMap _tempoMap;

    public bool WasCalled { get; private set; }
    public double[]? ReceivedOnsets { get; private set; }

    public FakeTempoDetector(TempoMap tempoMap)
    {
        _tempoMap = tempoMap;
    }

    public TempoMap? DetectTempo(ReadOnlySpan<double> onsets)
    {
        WasCalled = true;
        ReceivedOnsets = onsets.ToArray();
        return _tempoMap;
    }
}