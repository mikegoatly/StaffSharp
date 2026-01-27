using Xunit.Abstractions;

namespace StaffSharp.TestHelpers;

public sealed class XUnitProgress(ITestOutputHelper outputHelper) : IProgress<ImportProgress>
{
    public void Report(ImportProgress value)
    {
        outputHelper.WriteLine($"Import Progress: {value.Message}");
    }
}
