namespace StaffSharp.Layout.Model;

using StaffSharp.Notation;

/// <summary>
/// Represents a positioned barline.
/// </summary>
public sealed class BarlineLayoutSymbol : LayoutSymbol
{
    public required BarlineType BarlineType { get; init; }
}