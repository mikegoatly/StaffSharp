using StaffSharp.Audio.Analysis.Quantization;
using StaffSharp.Performance;

namespace StaffSharp.Audio.Tests.Fakes;

internal sealed class FakeQuantizer : IQuantizer
{
    private readonly IReadOnlyList<QuantizedNoteEvent> _quantized;

    public bool WasCalled { get; private set; }
    public double[]? ReceivedOnsets { get; private set; }
    public int[]? ReceivedPitches { get; private set; }
    public TempoMap? ReceivedTempoMap { get; private set; }

    public FakeQuantizer(IReadOnlyList<QuantizedNoteEvent> quantized)
    {
        _quantized = quantized;
    }

    public IReadOnlyList<QuantizedNoteEvent>? Quantize(
        ReadOnlySpan<double> onsetTimes,
        ReadOnlySpan<int> pitches,
        TempoMap tempoMap)
    {
        WasCalled = true;
        ReceivedOnsets = onsetTimes.ToArray();
        ReceivedPitches = pitches.ToArray();
        ReceivedTempoMap = tempoMap;
        return _quantized;
    }
}