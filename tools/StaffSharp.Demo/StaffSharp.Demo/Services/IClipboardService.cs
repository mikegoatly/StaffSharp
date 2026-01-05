namespace StaffSharp.Demo.Services;

/// <summary>
/// Service for clipboard operations.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Copies text to the clipboard.
    /// </summary>
    Task SetTextAsync(string text);

    /// <summary>
    /// Gets whether the clipboard service is available.
    /// </summary>
    bool IsAvailable { get; }
}
