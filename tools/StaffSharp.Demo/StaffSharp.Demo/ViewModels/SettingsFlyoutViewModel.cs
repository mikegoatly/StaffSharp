using System;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace StaffSharp.Demo.ViewModels;

/// <summary>
/// ViewModel for the settings flyout panel.
/// </summary>
public partial class SettingsFlyoutViewModel : ViewModelBase
{
    [ObservableProperty]
    private ProcessingOptions _options;

    [ObservableProperty]
    private bool _isOpen;

    public SettingsFlyoutViewModel()
    {
        _options = new ProcessingOptions();
    }

    /// <summary>
    /// Event fired when settings should be applied.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1003:Use generic event handler instances", Justification = "Simple action is more convenient for this use case")]
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
    /// Opens the flyout.
    /// </summary>
    public void Open()
    {
        IsOpen = true;
    }

    /// <summary>
    /// Closes the flyout.
    /// </summary>
    public void Close()
    {
        IsOpen = false;
    }

    /// <summary>
    /// Applies the current settings and notifies listeners.
    /// </summary>
    [RelayCommand]
    private void ApplySettings()
    {
        SettingsApplied?.Invoke(Options);
        Close();
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
