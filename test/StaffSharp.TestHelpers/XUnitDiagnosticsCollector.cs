using StaffSharp.Audio.Diagnostics;

using Xunit.Abstractions;

namespace StaffSharp.TestHelpers;

/// <summary>
/// Diagnostics collector that outputs to xUnit test output.
/// Useful for integration tests to capture pipeline diagnostics.
/// </summary>
public sealed class XUnitDiagnosticsCollector(ITestOutputHelper outputHelper) : IDiagnosticsCollector
{
    public void Collect<T>(string stageName, string key, T value)
    {
        // Special handling for arrays
        if (value is Array array)
        {
            if (array.Length <= 20)
            {
                // For small arrays, show all values
                var values = string.Join(", ", array.Cast<object>().Select(v => $"{v:F3}"));
                outputHelper.WriteLine($"[{stageName}] Key: {key}, Value: [{values}]");
            }
            else
            {
                // For large arrays, show first 10 and last 10
                var first10 = string.Join(", ", array.Cast<object>().Take(10).Select(v => $"{v:F3}"));
                var last10 = string.Join(", ", array.Cast<object>().Skip(array.Length - 10).Select(v => $"{v:F3}"));
                outputHelper.WriteLine($"[{stageName}] Key: {key}, Count: {array.Length}, First 10: [{first10}], Last 10: [{last10}]");
            }
        }
        else
        {
            outputHelper.WriteLine($"[{stageName}] Key: {key}, Value: {value}");
        }
    }

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> GetDiagnostics()
    {
        throw new NotImplementedException();
    }
}
