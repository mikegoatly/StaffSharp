namespace StaffSharp.Svg;

using System.Xml.Linq;

using StaffSharp.Svg.Layout;

internal sealed class SymbolRenderer : LayoutElementRenderer<LayoutSymbol>
{
    public static SymbolRenderer Instance { get; } = new();
    public override XElement Render(LayoutSymbol symbol, SvgContext context)
    {
        return symbol switch
        {
            NoteLayoutSymbol noteSymbol => NoteRenderer.Instance.Render(noteSymbol, context),
            RestLayoutSymbol restSymbol => RestRenderer.Instance.Render(restSymbol, context),
            ChordLayoutSymbol chordSymbol => ChordRenderer.Instance.Render(chordSymbol, context),
            ClefLayoutSymbol clefSymbol => ClefRenderer.Instance.Render(clefSymbol, context),
            KeySignatureLayoutSymbol keySymbol => KeySignatureRenderer.Instance.Render(keySymbol, context),
            TimeSignatureLayoutSymbol timeSymbol => TimeSignatureRenderer.Instance.Render(timeSymbol, context),
            BarlineLayoutSymbol barlineSymbol => BarlineRenderer.Instance.Render(barlineSymbol, context),
            _ => throw new NotImplementedException()
        };
    }
}
