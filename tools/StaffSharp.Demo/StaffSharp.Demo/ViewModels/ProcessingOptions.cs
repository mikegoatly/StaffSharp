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

    // Note Detection
    [ObservableProperty]
    public partial bool UseMachineLearning { get; set; } = true;

    /// <summary>
    /// Resets all options to their default values.
    /// </summary>
    public void ResetToDefaults()
    {
        ExportOptions = new SvgExportOptions();
        UseMachineLearning = true;
    }
}
