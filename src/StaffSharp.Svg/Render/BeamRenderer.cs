namespace StaffSharp.Render;

using System.Xml.Linq;

using StaffSharp;
using StaffSharp.Layout.Model;

internal sealed class BeamRenderer : LayoutElementRenderer<IGrouping<int, IStemmedSymbol>>
{
    public static BeamRenderer Instance { get; } = new();

    public override XElement Render(IGrouping<int, IStemmedSymbol> beamGroup, SvgContext context)
    {
        var group = new XElement(SvgNamespace + "g",
            new XAttribute("class", "beam-group")
        );

        var symbols = beamGroup.OrderBy(s => s.TimePosition).ToList();
        if (symbols.Count < 2) return group;

        var stemUp = symbols[0].Stem.Up;
        var beamThickness = context.HalfStaffSpace;
        var beamGap = 0.25 * context.StaffSpace;

        // Use pre-calculated beam positions from layout (stored in StemY2 and StemX)
        var firstSymbol = symbols.First();
        var lastSymbol = symbols.Last();
        var beamY1 = firstSymbol.Stem.Y2;
        var beamY2 = lastSymbol.Stem.Y2;
        var beamX1 = firstSymbol.Stem.X;
        var beamX2 = lastSymbol.Stem.X;

        // Calculate the number of primary beams (shared by all notes in the group)
        var primaryBeamCount = symbols.Min(s => s.Beam.BeamCount);

        // Render primary beams (those that connect all notes)
        for (int beamIndex = 0; beamIndex < primaryBeamCount; beamIndex++)
        {
            var yOffset = beamIndex * (beamThickness + beamGap);

            // For stems up: beams go below the stem endpoint and stack downward
            // For stems down: beams go above the stem endpoint and stack upward
            var y1 = stemUp ? beamY1 + yOffset : beamY1 - yOffset - beamThickness;
            var y2 = stemUp ? beamY2 + yOffset : beamY2 - yOffset - beamThickness;

            // Use a polygon to create a slanted beam
            var points = stemUp
                ? $"{beamX1},{y1} {beamX2},{y2} {beamX2},{y2 + beamThickness} {beamX1},{y1 + beamThickness}"
                : $"{beamX1},{y1} {beamX2},{y2} {beamX2},{y2 + beamThickness} {beamX1},{y1 + beamThickness}";

            group.Add(new XElement(SvgNamespace + "polygon",
                new XAttribute("points", points),
                new XAttribute("fill", "black")
            ));
        }

        // Handle partial beams for notes with more beams than the primary count
        RenderPartialBeams(group, symbols, primaryBeamCount, beamY1, beamY2, stemUp, beamThickness, beamGap, context);

        return group;
    }

    private static void RenderPartialBeams(
        XElement group,
        List<IStemmedSymbol> symbols,
        int primaryBeamCount,
        double beamY1,
        double beamY2,
        bool stemUp,
        double beamThickness,
        double beamGap,
        SvgContext context)
    {
        // Calculate the slope of the primary beam for interpolation
        var firstSymbol = symbols.First();
        var lastSymbol = symbols.Last();
        var beamSlope = (beamY2 - beamY1) / (lastSymbol.Stem.X - firstSymbol.Stem.X);

        for (int i = 0; i < symbols.Count; i++)
        {
            var symbol = symbols[i];
            if (symbol.Beam.BeamCount <= primaryBeamCount) continue;

            // This note needs additional partial beams
            var partialBeamCount = symbol.Beam.BeamCount - primaryBeamCount;

            // Determine the direction of the partial beam
            // Partial beams extend toward the center of the group, or toward the next/previous note
            bool extendRight = i == 0 || (i < symbols.Count - 1 && symbols[i + 1].Beam.BeamCount > primaryBeamCount);
            bool extendLeft = i == symbols.Count - 1 || (i > 0 && symbols[i - 1].Beam.BeamCount > primaryBeamCount);

            // Interpolate the beam Y position at this note's stem X position
            var symbolBeamY = beamY1 + (symbol.Stem.X - firstSymbol.Stem.X) * beamSlope;

            for (int beamIndex = 0; beamIndex < partialBeamCount; beamIndex++)
            {
                var beamLevel = primaryBeamCount + beamIndex;
                var yOffset = beamLevel * (beamThickness + beamGap);
                var y = stemUp ? symbolBeamY + yOffset : symbolBeamY - yOffset - beamThickness;

                // Partial beam length (typically about half a space)
                var partialLength = 0.75 * context.StaffSpace;

                double x1, x2, y1, y2;

                if (extendRight && !extendLeft)
                {
                    // Extend to the right only
                    x1 = symbol.Stem.X;
                    x2 = symbol.Stem.X + partialLength;
                    y1 = y;
                    y2 = y + (partialLength * beamSlope);
                }
                else if (extendLeft && !extendRight)
                {
                    // Extend to the left only
                    x1 = symbol.Stem.X - partialLength;
                    x2 = symbol.Stem.X;
                    y1 = y - (partialLength * beamSlope);
                    y2 = y;
                }
                else
                {
                    // Extend both ways (centered) or default to left
                    var halfLength = partialLength / 2;
                    x1 = symbol.Stem.X - halfLength;
                    x2 = symbol.Stem.X + halfLength;
                    y1 = y - (halfLength * beamSlope);
                    y2 = y + (halfLength * beamSlope);
                }

                // Create a slanted partial beam using polygon
                var points = stemUp
                    ? $"{x1:F2},{y1:F2} " +
                      $"{x2:F2},{y2:F2} " +
                      $"{x2:F2},{y2 + beamThickness:F2)} " +
                      $"{x1:F2},{y1 + beamThickness:F2)}"
                    : $"{x1:F2},{y1:F2} " +
                      $"{x2:F2},{y2:F2} " +
                      $"{x2:F2},{y2 + beamThickness:F2)} " +
                      $"{x1:F2},{y1 + beamThickness:F2)}";

                group.Add(new XElement(SvgNamespace + "polygon",
                    new XAttribute("points", points),
                    new XAttribute("fill", "black")
                ));
            }
        }
    }
}
