namespace StaffSharp.Svg.Layout;

using StaffSharp.Notation;

/// <summary>
/// Represents a positioned rest.
/// </summary>
public sealed class RestLayoutSymbol : AugmentationDottedLayoutSymbol
{
    public required Rest Rest { get; init; }
}
