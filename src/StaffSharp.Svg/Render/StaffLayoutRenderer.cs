namespace StaffSharp.Render;

using System.Xml.Linq;

using StaffSharp;
using StaffSharp.Layout.Model;

internal class StaffLayoutRenderer : LayoutElementRenderer<LayoutStaff>
{
    public static StaffLayoutRenderer Instance { get; } = new();

    public override XElement Render(LayoutStaff staff, SvgContext context)
    {
        var staffY = staff.Bounds.Y;
        var group = new XElement(SvgNamespace + "g",
            new XAttribute("class", "staff"),
            new XAttribute("transform", CreateTranslate(0, staffY))
        );

        // Draw 5 staff lines
        var staffX = staff.Bounds.X;
        var staffWidth = staff.Bounds.Width;
        for (int i = 0; i < 5; i++)
        {
            var y = i * context.StaffSpace;
            var x1 = staffX;
            var x2 = staffX + staffWidth;
            group.Add(CreateLine(x1, y, x2, y, strokeWidth: 0.5));
        }

        if (context.RenderDebugArtifacts)
        {
            AddBoundsRectangle(group, staff.Bounds with { Y = 0 }, "red");
        }

        // Render symbols
        foreach (var measure in staff.Measures)
        {
            foreach (var symbol in measure.Symbols)
            {
                group.Add(SymbolRenderer.Instance.Render(symbol, context));
            }

            // Render beams for this measure (after symbols so they appear on top)
            foreach (var beamGroup in measure.Symbols
                .OfType<IStemmedSymbol>()
                .Where(s => s.Beam.GroupId.HasValue)
                .GroupBy(s => s.Beam.GroupId!.Value)
                .ToList())
            {
                group.Add(BeamRenderer.Instance.Render(beamGroup, context));
            }

            // Render ties and slurs
            foreach (var curve in measure.Curves)
            {
                group.Add(CurveRenderer.Instance.Render(curve, context));
            }

            if (context.RenderDebugArtifacts)
            {
                // Add a rectangle for the layout bounds
                AddBoundsRectangle(group, measure.Bounds, "blue");
            }
        }

        return group;
    }
}
