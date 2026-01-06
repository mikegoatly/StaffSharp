using StaffSharp.Audio.Diagnostics;

using Xunit.Abstractions;

namespace StaffSharp.Audio.Tests.IntegrationTests
{
    internal sealed class XUnitDiagnosticsCollector(ITestOutputHelper outputHelper) : IDiagnosticsCollector
    {
        public void Collect<T>(string stageName, string key, T value)
        {
            outputHelper.WriteLine($"[{stageName}] Key: {key}, Value: {value}");
        }

        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> GetDiagnostics()
        {
            throw new NotImplementedException();
        }
    }
}
