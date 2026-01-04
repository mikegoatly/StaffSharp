namespace StaffSharp.Svg.Layout;

/// <summary>
/// Represents spacing (padding) around a layout symbol.
/// </summary>
/// <param name="Left">Left padding in units.</param>
/// <param name="Right">Right padding in units.</param>
public readonly record struct LayoutSpacing(double Left, double Right)
{
    /// <summary>
    /// Creates a LayoutSpacing with equal left and right spacing.
    /// </summary>
    public LayoutSpacing(double spacing) : this(spacing, spacing)
    {
    }
}
