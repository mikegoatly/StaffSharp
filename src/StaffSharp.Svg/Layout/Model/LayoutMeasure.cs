namespace StaffSharp.Layout.Model;

using StaffSharp.Notation;

// TODO make these internal and expose List<T> rather than IReadOnlyList<T> for performance
/// <summary>
/// Represents a measure within a staff.
/// </summary>
public class LayoutMeasure : LayoutElement
{
    public IReadOnlyList<LayoutSymbol> Symbols => _symbols;
    private readonly List<LayoutSymbol> _symbols = [];

    public IReadOnlyList<LayoutCurve> Curves => _curves;
    private readonly List<LayoutCurve> _curves = [];

    /// <summary>
    /// The time signature for this measure, if it differs from the score's default.
    /// </summary>
    public TimeSignature? TimeSignature { get; set; }

    internal void AddSymbol(LayoutSymbol symbol) => _symbols.Add(symbol);
    internal void InsertSymbol(int index, LayoutSymbol symbol) => _symbols.Insert(index, symbol);
    internal void AddCurve(LayoutCurve curve) => _curves.Add(curve);
}