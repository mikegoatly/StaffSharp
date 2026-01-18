using System.Collections.Concurrent;

namespace StaffSharp.Audio.Diagnostics;

/// <summary>
/// In-memory diagnostics collector using thread-safe concurrent dictionaries.
/// Stores all diagnostic data in memory for later export or inspection.
/// </summary>
public sealed class InMemoryDiagnosticsCollector : IDiagnosticsCollector
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object>> _data = new();

    /// <summary>
    /// Collects a diagnostic value. Thread-safe.
    /// </summary>
    public void Collect<T>(string stageName, string key, T value)
    {
        ArgumentNullException.ThrowIfNull(stageName);
        ArgumentNullException.ThrowIfNull(key);

        var stageData = _data.GetOrAdd(stageName, _ => new ConcurrentDictionary<string, object>());
        stageData[key] = value!;
    }

    /// <summary>
    /// Gets all collected diagnostics.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> GetDiagnostics()
    {
        // Return a snapshot to prevent concurrent modification issues
        return _data.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyDictionary<string, object>)new Dictionary<string, object>(kvp.Value));
    }

    /// <summary>
    /// Clears all collected diagnostics.
    /// </summary>
    public void Clear()
    {
        _data.Clear();
    }

    public T? GetDiagnostic<T>(string v)
    {
        foreach (var section in _data)
        {
            if (section.Value.TryGetValue(v, out var value) && value is T typedValue)
            {
                return typedValue;
            }
        }

        return default;
    }
}
