namespace StaffSharp.Svg.Layout;

/// <summary>
/// Interface for layout passes.
/// </summary>
public interface ILayoutPass
{
    void Run(LayoutModel model, SvgContext context);
}