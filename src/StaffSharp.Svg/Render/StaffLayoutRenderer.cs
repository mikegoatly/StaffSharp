namespace StaffSharp.Render;

using System.Xml.Linq;

using StaffSharp;
using StaffSharp.Layout.Model;

internal class StaffLayoutRenderer : LayoutElementRenderer<LayoutStaff>
{
    public static StaffLayoutRenderer Instance { get; } = new();

    public override XElement Render(LayoutStaff staff, SvgContext context)
    {
        var group = new XElement(SvgNamespace + "g",
            new XAttribute("class", "staff"),
            new XAttribute("transform", CreateTranslate(0, staff.Y))
        );

        // Draw 5 staff lines
        for (int i = 0; i < 5; i++)
        {
            var y = i * context.StaffSpace;
            var x1 = staff.X;
            var x2 = staff.X + staff.Width;
            group.Add(CreateLine(x1, y, x2, y));
        }

        // Render symbols
        foreach (var measure in staff.Measures)
        {
            foreach (var symbol in measure.Symbols)
            {
                var rendered = SymbolRenderer.Instance.Render(symbol, context);
                if (rendered != null)
                {
                    group.Add(rendered);
                }
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
        }

        return group;
    }
}
