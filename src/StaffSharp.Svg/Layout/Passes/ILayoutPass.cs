namespace StaffSharp.Layout.Passes;

/// <summary>
/// Interface for layout passes.
/// </summary>
public interface ILayoutPass
{
    void Run(LayoutModel model, SvgContext context);
}