using Spectre.Console;

using StaffSharp.Audio.Diagnostics;

namespace StaffSharp.Cli;

internal sealed class CliDiagnosticsCollector : IDiagnosticsCollector
{
    internal static CliDiagnosticsCollector Instance { get; } = new();

    public int MaxArrayLengthForDisplay { get; set; } = 20;
    public Dictionary<string, (int offset, int length)> ArrayOffsetsByKey { get; } = [];
    public HashSet<string> Filters { get; } = new(StringComparer.OrdinalIgnoreCase);

    public void Collect<T>(string stageName, string key, T value)
    {
        if (Filters.Count > 0 && !Filters.Contains(key))
        {
            return;
        }

        // Format arrays nicely
        string formattedValue;
        if (value is Array array)
        {
            int offset = 0;
            int length = MaxArrayLengthForDisplay;
            if (ArrayOffsetsByKey.TryGetValue(key, out var explicitItemConfig))
            {
                offset = explicitItemConfig.offset;
                length = explicitItemConfig.length;
            }

            var items = new List<string>(); 
            foreach (var item in array.Cast<object?>().Skip(offset).Take(length))
            {
                items.Add(item?.ToString() ?? "null");
            }

            formattedValue = $"[{(offset > 0 ? $"{{{offset} omitted}} " : "" )}{string.Join(", ", items)}{(array.Length > offset + length ? $" {{... {array.Length - (offset + length)} omitted}}" : "")}]";
        }
        else
        {
            formattedValue = value?.ToString() ?? "null";
        }
        
        // We'll just emit to console for now.
        AnsiConsole.MarkupLine($"[blue]{stageName}[/] [cyan]{key}[/]: [yellow]{Markup.Escape(formattedValue)}[/]");
    }

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> GetDiagnostics()
    {
        // No-op for CLI - we've already emitted everything.
        return new Dictionary<string, IReadOnlyDictionary<string, object>>();
    }
}
