using System;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace StaffSharp.Demo.ViewModels;

/// <summary>
/// ViewModel for the settings flyout panel.
/// </summary>
public partial class SettingsFlyoutViewModel : ViewModelBase
{
    public SettingsFlyoutViewModel()
    {
        Options = new ProcessingOptions();
    }

    [ObservableProperty]
    public partial ProcessingOptions Options { get; set; }

    [ObservableProperty]
    public partial bool IsOpen { get; set; }

    /// <summary>
    /// Event fired when settings should be applied.
    /// </summary>
    public event Action<ProcessingOptions>? SettingsApplied;

    /// <summary>
    /// Toggles the flyout open/closed state.
    /// </summary>
    [RelayCommand]
    private void ToggleFlyout()
    {
        IsOpen = !IsOpen;
    }

    /// <summary>
    /// Applies the current settings and notifies listeners.
    /// </summary>
    [RelayCommand]
    private void ApplySettings()
    {
        SettingsApplied?.Invoke(Options);
    }

    /// <summary>
    /// Resets all settings to their default values.
    /// </summary>
    [RelayCommand]
    private void ResetToDefaults()
    {
        Options.ResetToDefaults();
    }
}
