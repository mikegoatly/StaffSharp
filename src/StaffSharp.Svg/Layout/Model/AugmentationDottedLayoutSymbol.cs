namespace StaffSharp.Svg.Layout;

/// <summary>
/// Base class for symbols that can have augmentation dots (notes, chords, rests).
/// </summary>
public abstract class AugmentationDottedLayoutSymbol : LayoutSymbol
{
    // Augmentation dots
    public int DotCount { get; set; }
    public IList<double> DotXPositions { get; } = [];
    public double DotY { get; set; }
}
