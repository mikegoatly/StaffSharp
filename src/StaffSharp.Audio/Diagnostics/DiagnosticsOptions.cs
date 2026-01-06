namespace StaffSharp.Audio.Diagnostics;

/// <summary>
/// Options classes can derive from this to pass around diagnostic settings.
/// </summary>
public abstract record DiagnosticsOptions
{
    internal IDiagnosticsCollector? DiagnosticsCollector { get; init; }
}
