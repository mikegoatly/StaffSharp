using Avalonia.Controls;

namespace StaffSharp.Demo.Services;

/// <summary>
/// Clipboard service implementation using Avalonia's TopLevel.
/// </summary>
public sealed class ClipboardService : IClipboardService
{
    private TopLevel? _topLevel;

    /// <summary>
    /// Gets the shared instance of the clipboard service.
    /// </summary>
    public static ClipboardService Instance { get; } = new ClipboardService();

    public bool IsAvailable => _topLevel?.Clipboard != null;

    /// <summary>
    /// Initializes the clipboard service with a TopLevel instance.
    /// </summary>
    public void Initialize(TopLevel? topLevel)
    {
        _topLevel = topLevel;
    }

    public async Task SetTextAsync(string text)
    {
        if (_topLevel?.Clipboard == null)
        {
            throw new InvalidOperationException("Clipboard not available. Ensure the service is initialized.");
        }

        await _topLevel.Clipboard.SetTextAsync(text);
    }
}
