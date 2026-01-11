using Avalonia.Controls;
using Avalonia.Platform.Storage;

using StaffSharp.Demo.Services;
using StaffSharp.Demo.ViewModels;

namespace StaffSharp.Demo.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Provide StorageProvider and initialize clipboard when the view is attached
        if (DataContext is MainViewModel vm && TopLevel.GetTopLevel(this) is { } topLevel)
        {
            vm.StorageProvider = topLevel.StorageProvider;

            // Initialize the static clipboard service instance
            ClipboardService.Instance.Initialize(topLevel);
        }
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        // Cleanup when view is detached
        if (DataContext is MainViewModel vm)
        {
            vm.Cleanup();
        }
    }
}