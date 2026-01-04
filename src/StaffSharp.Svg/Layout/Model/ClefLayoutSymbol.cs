namespace StaffSharp.Svg.Layout;

using StaffSharp.Notation;

/// <summary>
/// Represents a positioned clef.
/// </summary>
public sealed class ClefLayoutSymbol : LayoutSymbol
{
    public required Clef Clef { get; init; }
}
