namespace StaffSharp.Layout.Model;

using StaffSharp;

/// <summary>
/// Base class for layout elements.
/// </summary>
internal abstract class LayoutElement : ILayoutElement
{
    public Bounds Bounds { get; set; }

    /// <summary>
    /// Virtual implementation that does nothing by default.
    /// Override in derived classes to implement specific bounds calculation logic.
    /// </summary>
    public virtual void UpdateBounds(SvgContext context)
    {
        // Default: no-op
    }
}