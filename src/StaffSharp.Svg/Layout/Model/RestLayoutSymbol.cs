namespace StaffSharp.Layout.Model;

using StaffSharp.Notation;

/// <summary>
/// Represents a positioned rest.
/// </summary>
internal sealed class RestLayoutSymbol : AugmentationDottedLayoutSymbol
{
    public required Rest Rest { get; init; }
}
