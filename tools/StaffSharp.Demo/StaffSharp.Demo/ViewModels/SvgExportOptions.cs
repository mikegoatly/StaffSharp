using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;

namespace StaffSharp.Demo.ViewModels;

public partial class SvgExportOptions : ObservableObject
{
    [ObservableProperty]
    public partial int MaxWidth { get; set; } = 1200;

    [ObservableProperty]
    public partial bool RenderDebugArtifacts { get; set; } = false;

    public Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>
        {
            ["maxWidth"] = MaxWidth.ToString(CultureInfo.InvariantCulture),
            ["renderDebugArtifacts"] = RenderDebugArtifacts.ToString(CultureInfo.InvariantCulture)
        };
    }
}
