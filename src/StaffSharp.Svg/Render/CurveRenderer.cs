namespace StaffSharp.Render;

using System.Xml.Linq;

using StaffSharp;
using StaffSharp.Layout.Model;

internal sealed class CurveRenderer : LayoutElementRenderer<LayoutCurve>
{
    public static CurveRenderer Instance { get; } = new();

    public override XElement Render(LayoutCurve curve, SvgContext context)
    {
        // Calculate thickness based on engraving standards
        var thickness = curve.IsTie
            ? 0.18 * context.StaffSpace  // Ties thinner
            : 0.22 * context.StaffSpace; // Slurs thicker

        var sx = curve.Bounds.X;
        var sy = curve.Bounds.Y;
        var ex = curve.EndX;
        var ey = curve.EndY;

        // Calculate control point based on taper mode
        double cpx, cpy;

        // Determine curve bulge direction: curves above bulge up (negative Y), curves below bulge down (positive Y)
        var curveDirection = curve.CurveAbove ? -1 : 1;
        var splitCurveBulge = curveDirection * 0.5 * context.StaffSpace;

        if (curve.EndTaper == CurveEndTaper.Both)
        {
            // Normal curve with apex - use the apex to derive control point
            var ax = curve.ApexX;
            var ay = curve.ApexY;
            cpx = 2.0 * ax - 0.5 * (sx + ex);
            cpy = 2.0 * ay - 0.5 * (sy + ey);
        }
        else if (curve.EndTaper == CurveEndTaper.Start)
        {
            // Curve exits at end - control point offset from end creates smooth horizontal exit
            // For horizontal tangent at end: control Y must equal end Y
            // Control X at midpoint creates smooth curve with proper bulge
            cpx = (sx + ex) / 2.0;
            cpy = ey; // Same Y as end for horizontal tangent at exit
            thickness /= 2.0; // Tapered end thinner
        }
        else if (curve.EndTaper == CurveEndTaper.End)
        {
            // Curve enters at start - control point offset from start creates smooth horizontal entry
            // For horizontal tangent at start: control Y must equal start Y
            // Control X at midpoint creates smooth curve with proper bulge
            cpx = (sx + ex) / 2.0;
            cpy = sy; // Same Y as start for horizontal tangent at entry
            thickness /= 2.0; // Tapered end thinner
        }
        else // CurveEndTaper.None
        {
            // Middle segment - small bulge in center
            cpx = (sx + ex) / 2.0;
            cpy = (sy + ey) / 2.0 + splitCurveBulge;
            thickness /= 2.0; // Tapered end thinner
        }

        // Create offset control points for thickness
        var thicknessDirection = curve.CurveAbove ? 1 : -1;
        var cp1y = cpy + (thicknessDirection * thickness);  // Upper curve control
        var cp2y = cpy - (thicknessDirection * thickness);  // Lower curve control

        // Generate path based on EndTaper mode
        var path = curve.EndTaper switch
        {
            CurveEndTaper.Both => CreateBothTaperedPath(sx, sy, ex, ey, cpx, cp1y, cp2y),
            CurveEndTaper.Start => CreateStartTaperedPath(sx, sy, ex, ey, cpx, cp1y, cp2y, thickness, thicknessDirection),
            CurveEndTaper.End => CreateEndTaperedPath(sx, sy, ex, ey, cpx, cp1y, cp2y, thickness, thicknessDirection),
            CurveEndTaper.None => CreateSquarePath(sx, sy, ex, ey, cpx, cp1y, cp2y, thickness, thicknessDirection),
            _ => throw new InvalidOperationException($"Unknown taper mode: {curve.EndTaper}")
        };

        var className = curve.IsTie ? "tie" : "slur";

        return new XElement(SvgNamespace + "path",
            new XAttribute("d", path),
            new XAttribute("fill", "black"),
            new XAttribute("class", className));
    }

    private static string CreateBothTaperedPath(
        double sx, double sy, double ex, double ey,
        double cpx, double cp1y, double cp2y)
    {
        // Tapered on both ends (normal curve)
        return $"M {sx:F2} {sy:F2} " +
               $"Q {cpx:F2} {cp1y:F2}, {ex:F2} {ey:F2} " +
               $"Q {cpx:F2} {cp2y:F2}, {sx:F2} {sy:F2} Z";
    }

    private static string CreateStartTaperedPath(
        double sx, double sy, double ex, double ey,
        double cpx, double cp1y, double cp2y,
        double thickness, int direction)
    {
        // Tapered at start, square at end
        var endTopY = ey + (direction * thickness);
        var endBottomY = ey - (direction * thickness);

        return $"M {sx:F2} {sy:F2} " +
               $"Q {cpx:F2} {cp1y:F2}, {ex:F2} {endTopY:F2} " +
               $"L {ex:F2} {endBottomY:F2} " +
               $"Q {cpx:F2} {cp2y:F2}, {sx:F2} {sy:F2} Z";
    }

    private static string CreateEndTaperedPath(
        double sx, double sy, double ex, double ey,
        double cpx, double cp1y, double cp2y,
        double thickness, int direction)
    {
        // Square at start, tapered at end
        var startTopY = sy + (direction * thickness);
        var startBottomY = sy - (direction * thickness);

        return $"M {sx:F2} {startTopY:F2} " +
               $"Q {cpx:F2} {cp1y:F2}, {ex:F2} {ey:F2} " +
               $"Q {cpx:F2} {cp2y:F2}, {sx:F2} {startBottomY:F2} Z";
    }

    private static string CreateSquarePath(
        double sx, double sy, double ex, double ey,
        double cpx, double cp1y, double cp2y,
        double thickness, int direction)
    {
        // Square on both ends (cross-system middle segment)
        var startTopY = sy + (direction * thickness);
        var startBottomY = sy - (direction * thickness);
        var endTopY = ey + (direction * thickness);
        var endBottomY = ey - (direction * thickness);

        return $"M {sx:F2} {startTopY:F2} " +
               $"Q {cpx:F2} {cp1y:F2}, {ex:F2} {endTopY:F2} " +
               $"L {ex:F2} {endBottomY:F2} " +
               $"Q {cpx:F2} {cp2y:F2}, {sx:F2} {startBottomY:F2} Z";
    }
}
