namespace StaffSharp.Layout.Model;

using StaffSharp.Notation;

/// <summary>
/// Represents a positioned barline.
/// </summary>
internal sealed class BarlineLayoutSymbol : LayoutSymbol
{
    public required BarlineType BarlineType { get; init; }
}