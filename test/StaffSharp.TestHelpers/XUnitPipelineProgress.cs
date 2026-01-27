using StaffSharp.Audio.Pipeline;

using Xunit.Abstractions;

namespace StaffSharp.TestHelpers;

public static class XUnitPipelineProgress
{ 
    public static PipelineProgress Create(ITestOutputHelper outputHelper)
    {
        return new PipelineProgress(
            new XUnitProgress(outputHelper),
            new XUnitDiagnosticsCollector(outputHelper),
            "UnitTest");
    }
}
