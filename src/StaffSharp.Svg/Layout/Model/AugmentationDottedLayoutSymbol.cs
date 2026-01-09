namespace StaffSharp.Layout.Model;

/// <summary>
/// Base class for symbols that can have augmentation dots (notes, chords, rests).
/// </summary>
internal abstract class AugmentationDottedLayoutSymbol : LayoutSymbol
{
    // Augmentation dots
    public int DotCount { get; set; }
    public List<double> DotXPositions { get; } = [];
    public double DotY { get; set; }
}
