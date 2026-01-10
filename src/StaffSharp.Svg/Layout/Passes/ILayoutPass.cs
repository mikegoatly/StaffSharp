namespace StaffSharp.Layout.Passes;

/// <summary>
/// Interface for layout passes.
/// </summary>
internal interface ILayoutPass
{
    void Run(LayoutModel model, SvgContext context);
}