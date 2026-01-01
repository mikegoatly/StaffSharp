using StaffSharp.Audio.Diagnostics;

namespace StaffSharp.Audio.Tests;

/// <summary>
/// Simple in-memory diagnostics collector for testing.
/// </summary>
internal sealed class MemoryDiagnosticsCollector : IDiagnosticsCollector
{
    private readonly Dictionary<string, Dictionary<string, object>> _diagnostics = new();

    public void Collect<T>(string stageName, string key, T value)
    {
        if (!_diagnostics.TryGetValue(stageName, out var stageDict))
        {
            stageDict = new Dictionary<string, object>();
            _diagnostics[stageName] = stageDict;
        }

        stageDict[key] = value!;
    }

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> GetDiagnostics()
    {
        return _diagnostics.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyDictionary<string, object>)kvp.Value
        );
    }

    public IEnumerable<DiagnosticEntry> GetEntries()
    {
        foreach (var stage in _diagnostics)
        {
            foreach (var entry in stage.Value)
            {
                yield return new DiagnosticEntry(stage.Key, entry.Key, entry.Value);
            }
        }
    }
}

internal sealed record DiagnosticEntry(string StageName, string Key, object Value);
