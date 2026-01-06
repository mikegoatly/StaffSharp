using Spectre.Console;

using StaffSharp.Audio.Diagnostics;

namespace StaffSharp.Cli;

internal sealed class CliDiagnosticsCollector : IDiagnosticsCollector
{
    public void Collect<T>(string stageName, string key, T value)
    {
        // We'll just emit to console for now.
        AnsiConsole.MarkupLine($"[blue]{stageName}[/] [cyan]{key}[/]: [yellow]{Markup.Escape(value?.ToString() ?? "null")}[/]");

    }

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> GetDiagnostics()
    {
        // No-op for CLI - we've already emitted everything.
        return new Dictionary<string, IReadOnlyDictionary<string, object>>();
    }
}
