namespace StaffSharp.Svg.Layout;

/// <summary>
/// Base class for layout elements.
/// </summary>
public abstract class LayoutElement : ILayoutElement
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}