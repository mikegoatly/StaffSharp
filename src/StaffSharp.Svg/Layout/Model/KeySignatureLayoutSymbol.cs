namespace StaffSharp.Svg.Layout;

using StaffSharp.Notation;

/// <summary>
/// Represents a positioned key signature.
/// </summary>
public sealed class KeySignatureLayoutSymbol : LayoutSymbol
{
    public required KeySignature KeySignature { get; init; }
    public Clef Clef { get; init; } = Clef.Treble;
}
