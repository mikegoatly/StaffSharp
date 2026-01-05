using Avalonia.Controls;
using Avalonia.Platform.Storage;

using StaffSharp.Demo.ViewModels;

namespace StaffSharp.Demo.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.PickFileAsync = PickFileAsync;
            viewModel.SaveFileAsync = SaveFileAsync;
        }
    }

    private async Task<string?> PickFileAsync(string[] extensions, string title)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var fileTypes = new FilePickerFileType(title)
        {
            Patterns = extensions.Select(ext => $"*.{ext}").ToArray()
        };

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            FileTypeFilter = new[] { fileTypes },
            AllowMultiple = false
        });

        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }

    private async Task<string?> SaveFileAsync(string[] extensions, string title)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var fileTypes = new FilePickerFileType(title)
        {
            Patterns = extensions.Select(ext => $"*.{ext}").ToArray()
        };

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            FileTypeChoices = new[] { fileTypes }
        });

        return file?.Path.LocalPath;
    }
}