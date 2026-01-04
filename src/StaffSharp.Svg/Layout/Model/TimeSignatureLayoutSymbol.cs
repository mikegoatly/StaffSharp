namespace StaffSharp.Svg.Layout;

using StaffSharp.Notation;

/// <summary>
/// Represents a positioned time signature.
/// </summary>
public sealed class TimeSignatureLayoutSymbol : LayoutSymbol
{
    public required TimeSignature TimeSignature { get; init; }
}
