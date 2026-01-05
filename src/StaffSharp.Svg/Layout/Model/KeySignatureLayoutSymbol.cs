namespace StaffSharp.Layout.Model;

using StaffSharp.Layout.Services;
using StaffSharp.Notation;

/// <summary>
/// Represents a positioned key signature.
/// </summary>
public sealed class KeySignatureLayoutSymbol : LayoutSymbol
{
    public required KeySignature KeySignature { get; init; }
    public Clef Clef { get; init; } = Clef.Treble;

    internal static LayoutSymbol Create(KeySignature keySignature, Clef clef, SvgContext context)
    {
        return new KeySignatureLayoutSymbol
        {
            KeySignature = keySignature,
            Clef = clef,
            TimePosition = -2.0,
            Width = KeySignatureService.CalculateWidth(keySignature, context),
            Spacing = KeySignatureService.KeySignatureSpacing(context)
        };
    }
}
