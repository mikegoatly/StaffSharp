using CommunityToolkit.Mvvm.ComponentModel;

namespace StaffSharp.Demo.ViewModels;

/// <summary>
/// Configuration options for audio-to-score processing.
/// </summary>
public partial class ProcessingOptions : ObservableObject
{
    // SVG Rendering
    [ObservableProperty]
    public partial SvgExportOptions ExportOptions { get; set; } = new SvgExportOptions();

    /// <summary>
    /// Resets all options to their default values.
    /// </summary>
    public void ResetToDefaults()
    {
        ExportOptions = new SvgExportOptions();
    }
}
