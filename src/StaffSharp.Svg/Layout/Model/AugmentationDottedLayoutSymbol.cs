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

    public override void Offset(double dx, double dy)
    {
        base.Offset(dx, dy);

        // Offset dot positions
        DotY += dy;
        for (int i = 0; i < DotXPositions.Count; i++)
        {
            DotXPositions[i] += dx;
        }
    }
}
