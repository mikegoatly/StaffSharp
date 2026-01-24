namespace StaffSharp.Layout.Model;

using StaffSharp.Layout;
using StaffSharp.Notation;

internal abstract class StemmedSymbol : AugmentationDottedLayoutSymbol, IStemmedSymbol
{
    public StemInfo Stem { get; set; }
    public BeamInfo Beam { get; set; }
    public Bounds NoteHeadBounds { get; set; }

    public abstract SymbolicDuration Duration { get; }

    public abstract double GetEffectiveBottomY();
    public abstract double GetEffectiveTopY();

    public List<(Decoration Type, Bounds Bounds)> Decorations { get; } = [];

    public override void Offset(double dx, double dy)
    {
        base.Offset(dx, dy);

        // Offset note head bounds
        NoteHeadBounds = NoteHeadBounds.Offset(dx, dy);

        // Offset stem positions
        Stem = Stem with { Y1 = Stem.Y1 + dy, Y2 = Stem.Y2 + dy };

        // Offset positioned decorations
        for (int i = 0; i < Decorations.Count; i++)
        {
            var decoration = Decorations[i];
            Decorations[i] = decoration with { Bounds = decoration.Bounds.Offset(dx, dy) };
        }
    }
}
