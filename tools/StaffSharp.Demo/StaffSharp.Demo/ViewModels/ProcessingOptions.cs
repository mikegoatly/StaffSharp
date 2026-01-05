using CommunityToolkit.Mvvm.ComponentModel;

namespace StaffSharp.Demo.ViewModels;

/// <summary>
/// Configuration options for audio-to-score processing.
/// </summary>
public partial class ProcessingOptions : ObservableObject
{
    // YIN Pitch Detection Parameters
    [ObservableProperty]
    public partial double YinThreshold { get; set; } = 0.15; // Lower = more sensitive, higher = more selective

    // Onset Detection Parameters
    [ObservableProperty]
    public partial double OnsetThreshold { get; set; } = 1.5; // Spectral flux threshold multiplier

    [ObservableProperty]
    public partial int OnsetWindowSize { get; set; } = 2048; // FFT window size for onset detection

    // Tempo Detection
    [ObservableProperty]
    public partial int TempoHint { get; set; } = 0; // 0 = auto-detect
    
    // Quantization
    [ObservableProperty]
    public partial int Subdivision { get; set; } = 16; // Subdivision for quantization (e.g., 16 = sixteenth notes)

    // SVG Rendering
    [ObservableProperty]
    public partial int SvgWidth { get; set; } = 1200;

    [ObservableProperty]
    public partial bool ShowDebugBounds { get; set; } = false;

    /// <summary>
    /// Resets all options to their default values.
    /// </summary>
    public void ResetToDefaults()
    {
        YinThreshold = 0.15;
        OnsetThreshold = 1.5;
        OnsetWindowSize = 2048;
        TempoHint = 0;
        Subdivision = 16;
        SvgWidth = 1200;
        ShowDebugBounds = false;
    }

    /// <summary>
    /// Creates a copy of these options.
    /// </summary>
    public ProcessingOptions Clone()
    {
        return new ProcessingOptions
        {
            YinThreshold = YinThreshold,
            OnsetThreshold = OnsetThreshold,
            OnsetWindowSize = OnsetWindowSize,
            TempoHint = TempoHint,
            Subdivision = Subdivision,
            SvgWidth = SvgWidth,
            ShowDebugBounds = ShowDebugBounds
        };
    }
}
