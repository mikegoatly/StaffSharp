namespace StaffSharp.Layout.Model;

internal enum CurveEndTaper
{
    None = 0,    // Square ends (cross-system middle segment)
    Start = 1,   // Tapered at start, square at end
    End = 2,     // Square at start, tapered at end
    Both = 3     // Tapered on both ends (normal)
}

/// <summary>
/// Represents a tie or slur curve in the layout.
/// </summary>
internal class LayoutCurve : LayoutElement
{
    /// <summary>
    /// True if this is a tie (same pitch), false if slur (different pitches).
    /// </summary>
    public bool IsTie { get; set; }

    /// <summary>
    /// True if the curve should arc above the notes, false if below.
    /// </summary>
    public bool CurveAbove { get; set; }

    /// <summary>
    /// X coordinate of the curve end.
    /// </summary>
    public double EndX { get; set; }

    /// <summary>
    /// Y coordinate of the curve end.
    /// </summary>
    public double EndY { get; set; }

    /// <summary>
    /// Y coordinate of the apex point (highest/lowest point of the curve).
    /// </summary>
    public double ApexY { get; set; }

    /// <summary>
    /// X coordinate of the apex point (midpoint between start and end).
    /// </summary>
    public double ApexX => Bounds.X + (Bounds.Width / 2.0);

    /// <summary>
    /// Specifies which ends of the curve should be tapered (pointed) vs square.
    /// </summary>
    public CurveEndTaper EndTaper { get; init; }

    internal static LayoutCurve Create(
        IStemmedSymbol referenceNote,
        double startX,
        double startY,
        double endX,
        double endY,
        SvgContext context,
        CurveEndTaper endTaper,
        bool isTie)
    {
        // Determine curve direction based on stem direction
        // If stems are up, curve goes below; if stems are down, curve goes above
        var curveAbove = !referenceNote.Stem.Up;

        // Position curve above or below the notehead, not through the middle
        // Add small clearance (0.15 staff spaces) from the notehead edge
        var offsetMagnitude = context.StaffSpace * 0.65;
        var verticalOffset = curveAbove ? -offsetMagnitude : offsetMagnitude;

        startY += verticalOffset;
        endY += verticalOffset;

        // Calculate apex Y position based on curve height
        var curveHeight = isTie ? 0.5 * context.StaffSpace : 0.7 * context.StaffSpace;
        var controlYOffset = curveAbove ? -curveHeight : curveHeight;
        // For curve above (stem down): apex should be above (smaller Y) both endpoints
        // For curve below (stem up): apex should be below (larger Y) both endpoints
        var apexY = (curveAbove
            ? Math.Min(startY, endY)
            : Math.Max(startY, endY)) + controlYOffset;

        return new LayoutCurve
        {
            IsTie = isTie,
            CurveAbove = curveAbove,
            Bounds = new(startX, startY, Math.Abs(endX - startX), Math.Abs(endY - startY)),
            EndX = endX,
            EndY = endY,
            EndTaper = endTaper,
            ApexY = apexY
        };
    }

    /// <summary>
    /// Creates a curve segment that spans an entire system when a tie/slur
    /// continues from a previous system to a subsequent system.
    /// </summary>
    internal static LayoutCurve CreateCrossSystem(
        LayoutSystem system,
        SvgContext context,
        bool isTie)
    {
        // Position above the staff to avoid barlines
        // Staff top line is at Y=0, so negative Y is above the staff
        var y = system.Bounds.Y - (context.StaffSpace * 1.5);

        return new LayoutCurve
        {
            IsTie = isTie,
            CurveAbove = true,
            Bounds = new(system.Bounds.X, y, system.Bounds.Width, 2.0),
            EndX = system.Bounds.X + system.Bounds.Width,
            EndY = y,
            EndTaper = CurveEndTaper.None,
            ApexY = y + 2.0
        };
    }

    public override void Offset(double dx, double dy)
    {
        base.Offset(dx, dy);

        // Offset curve end and apex positions
        EndX += dx;
        EndY += dy;
        ApexY += dy;
    }
}
