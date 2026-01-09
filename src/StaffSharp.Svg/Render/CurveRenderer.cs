namespace StaffSharp.Render;

using System.Globalization;
using System.Xml.Linq;

using StaffSharp;
using StaffSharp.Layout.Model;

internal sealed class CurveRenderer : LayoutElementRenderer<LayoutCurve>
{
    public static CurveRenderer Instance { get; } = new();
    public override XElement Render(LayoutCurve curve, SvgContext context)
    {
        // Create a filled tie shape with tapered ends
        // Thickness in the middle, tapered to points at the ends
        var thickness = 0.15 * context.StaffSpace; // Tie thickness at the thickest point
        var direction = curve.CurveAbove ? -1 : 1;

        // Create the tie as two Bézier curves forming a closed shape
        // Top curve (from start to end)
        var topPath = $"M {curve.StartX.ToString(CultureInfo.InvariantCulture)} {curve.StartY.ToString(CultureInfo.InvariantCulture)} " +
                      $"C {curve.ControlX1.ToString(CultureInfo.InvariantCulture)} {curve.ControlY1.ToString(CultureInfo.InvariantCulture)}, " +
                      $"{curve.ControlX2.ToString(CultureInfo.InvariantCulture)} {curve.ControlY2.ToString(CultureInfo.InvariantCulture)}, " +
                      $"{curve.EndX.ToString(CultureInfo.InvariantCulture)} {curve.EndY.ToString(CultureInfo.InvariantCulture)}";

        // Bottom curve (from end back to start, with thickness offset)
        var bottomStartY = curve.EndY + (direction * thickness);
        var bottomEndY = curve.StartY + (direction * thickness);
        var bottomControl1Y = curve.ControlY2 + (direction * thickness);
        var bottomControl2Y = curve.ControlY1 + (direction * thickness);

        var bottomPath = $" L {curve.EndX.ToString(CultureInfo.InvariantCulture)} {bottomStartY.ToString(CultureInfo.InvariantCulture)} " +
                        $"C {curve.ControlX2.ToString(CultureInfo.InvariantCulture)} {bottomControl1Y.ToString(CultureInfo.InvariantCulture)}, " +
                        $"{curve.ControlX1.ToString(CultureInfo.InvariantCulture)} {bottomControl2Y.ToString(CultureInfo.InvariantCulture)}, " +
                        $"{curve.StartX.ToString(CultureInfo.InvariantCulture)} {bottomEndY.ToString(CultureInfo.InvariantCulture)} Z";

            var className = curve.IsTie ? "tie" : "slur";
            if (curve.ContinuationStart) className += " slur-cont-start";
            if (curve.ContinuationEnd) className += " slur-cont-end";

            return new XElement(SvgNamespace + "path",
                new XAttribute("d", topPath + bottomPath),
                new XAttribute("fill", "black"),
                new XAttribute("class", className));
    }
}
