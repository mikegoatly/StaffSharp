namespace StaffSharp.Layout.Model;

using StaffSharp;

/// <summary>
/// Base class for layout elements.
/// </summary>
internal abstract class LayoutElement : ILayoutElement
{
    public Bounds Bounds { get; set; }

    /// <summary>
    /// Offsets the bounds by the given amounts.
    /// Override in derived classes to also offset child elements.
    /// </summary>
    public virtual void Offset(double dx, double dy)
    {
        Bounds = Bounds.Offset(dx, dy);
    }
}