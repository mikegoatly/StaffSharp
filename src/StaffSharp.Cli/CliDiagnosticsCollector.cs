using Spectre.Console;

using StaffSharp.Audio.Diagnostics;

namespace StaffSharp.Cli;

internal sealed class CliDiagnosticsCollector : IDiagnosticsCollector
{
    public void Collect<T>(string stageName, string key, T value)
    {
        // Format arrays nicely
        string formattedValue;
        if (value is Array array)
        {
            var items = new List<string>();
            foreach (var item in array)
            {
                items.Add(item?.ToString() ?? "null");
            }
            formattedValue = $"[{string.Join(", ", items)}]";
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
