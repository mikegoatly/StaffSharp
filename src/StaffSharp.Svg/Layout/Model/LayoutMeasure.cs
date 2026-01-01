namespace StaffSharp.Svg.Layout;

/// <summary>
/// Represents a measure within a staff.
/// </summary>
public class LayoutMeasure : LayoutElement
{
    public IReadOnlyList<LayoutSymbol> Symbols => _symbols;
    private readonly List<LayoutSymbol> _symbols = new();

    public IReadOnlyList<LayoutCurve> Curves => _curves;
    private readonly List<LayoutCurve> _curves = new();

    internal void AddSymbol(LayoutSymbol symbol) => _symbols.Add(symbol);
    internal void AddCurve(LayoutCurve curve) => _curves.Add(curve);
}