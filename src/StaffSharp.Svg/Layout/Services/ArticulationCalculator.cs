namespace StaffSharp.Svg.Layout.Services;

using StaffSharp.Layout.Model;
using StaffSharp.Notation;
using StaffSharp.Svg;

/// <summary>
/// Calculates positions for articulations and decorations on notes and chords.
/// Follows standard music engraving practices.
/// </summary>
internal static class ArticulationCalculator
{
    // Base offsets from stem/notehead edge for different articulation types (in staff spaces)
    private const double StaccatoOffset = 0.7;      // Close articulations (staccato, tenuto, accent)
    private const double OrnamentOffset = 1.5;      // Ornaments (trill, turn, mordent)
    private const double FermataOffset = 2.5;       // Fermata (needs more space)

    // Vertical spacing between stacked articulations (in staff spaces)
    private const double ArticulationStackSpacing = 0.6;

    /// <summary>
    /// Determines which side of the note an articulation should be placed.
    /// </summary>
    private enum ArticulationSide
    {
        AboveNote,      // Standard position above notehead
        BelowNote,      // Standard position below notehead
        OppositeToStem  // On the side opposite the stem (default for most)
    }

    /// <summary>
    /// Gets the placement priority for an articulation (lower = closer to notehead).
    /// Based on standard music engraving practices:
    /// 1. Close articulations (staccato, tenuto) - closest to notehead
    /// 2. Stress marks (accent, marcato)
    /// 3. Ornaments and bowing marks
    /// 4. Holds (fermata, breath) - farthest from notehead
    /// </summary>
    private static int GetPlacementPriority(Decoration decoration)
    {
        return decoration switch
        {
            // Priority 1: Close articulations (closest to notehead)
            Decoration.Staccato => 1,
            Decoration.Tenuto => 1,

            // Priority 2: Stress/accent marks
            Decoration.Accent => 2,
            Decoration.Marcato => 2,

            // Priority 3: Ornaments and bowing marks
            Decoration.Trill => 3,
            Decoration.Turn => 3,
            Decoration.Mordent => 3,
            Decoration.UpperMordent => 3,
            Decoration.LowerMordent => 3,
            Decoration.InvertedTurn => 3,
            Decoration.Roll => 3,
            Decoration.UpBow => 3,
            Decoration.DownBow => 3,

            // Priority 4: Holds (farthest from notehead)
            Decoration.Fermata => 4,
            Decoration.Breath => 4,

            // Dynamics and pedal marks don't stack vertically with articulations
            _ => 99
        };
    }

    /// <summary>
    /// Gets the offset from the notehead edge for a specific articulation type.
    /// </summary>
    private static double GetArticulationOffset(Decoration decoration)
    {
        return decoration switch
        {
            // Close articulations
            Decoration.Staccato => StaccatoOffset,
            Decoration.Tenuto => StaccatoOffset,
            Decoration.Accent => StaccatoOffset,
            Decoration.Marcato => StaccatoOffset,
            
            // Fermata needs extra space
            Decoration.Fermata => FermataOffset,
            Decoration.Breath => FermataOffset,
            
            // Ornaments and bowing marks
            Decoration.Trill => OrnamentOffset,
            Decoration.Turn => OrnamentOffset,
            Decoration.Mordent => OrnamentOffset,
            Decoration.UpperMordent => OrnamentOffset,
            Decoration.LowerMordent => OrnamentOffset,
            Decoration.InvertedTurn => OrnamentOffset,
            Decoration.Roll => OrnamentOffset,
            Decoration.UpBow => OrnamentOffset,
            Decoration.DownBow => OrnamentOffset,
            
            // Default
            _ => StaccatoOffset
        };
    }

    /// <summary>
    /// Determines which side of the note the articulation should appear.
    /// </summary>
    private static ArticulationSide GetArticulationSide(Decoration decoration)
    {
        return decoration switch
        {
            // Fermata always goes above (standard practice)
            Decoration.Fermata => ArticulationSide.AboveNote,
            
            // Breath mark typically above
            Decoration.Breath => ArticulationSide.AboveNote,
            
            // All other articulations go opposite to stem
            _ => ArticulationSide.OppositeToStem
        };
    }

    /// <summary>
    /// Calculates articulation positions for a single note.
    /// </summary>
    /// <param name="symbol">The note symbol with stem and position information.</param>
    /// <param name="decorations">List of decorations to position.</param>
    /// <param name="context">SVG rendering context.</param>
    /// <returns>List of positioned decorations (Type, X, Y).</returns>
    public static List<(Decoration Type, double X, double Y)> CalculateArticulations(
        IStemmedSymbol symbol,
        IReadOnlyList<Decoration> decorations,
        SvgContext context)
    {
        if (decorations.Count == 0)
        {
            return [];
        }

        var result = new List<(Decoration Type, double X, double Y)>();

        // Filter to only articulations that should be positioned (exclude dynamics, pedal)
        var articulationsToPlace = decorations
            .Where(d => GetPlacementPriority(d) < 99)
            .OrderBy(GetPlacementPriority)
            .ToList();

        if (articulationsToPlace.Count == 0)
        {
            return result;
        }

        var noteX = symbol.X;
        var stemUp = symbol.Stem.Up;

        // Get the effective top and bottom boundaries (accounting for stem/beam)
        var effectiveTop = symbol.GetEffectiveTopY();
        var effectiveBottom = symbol.GetEffectiveBottomY();

        // Group by side (above or below)
        var aboveArticulations = new List<Decoration>();
        var belowArticulations = new List<Decoration>();

        foreach (var decoration in articulationsToPlace)
        {
            var side = GetArticulationSide(decoration);

            if (side == ArticulationSide.AboveNote)
            {
                aboveArticulations.Add(decoration);
            }
            else if (side == ArticulationSide.BelowNote)
            {
                belowArticulations.Add(decoration);
            }
            else // OppositeToStem
            {
                if (stemUp)
                {
                    belowArticulations.Add(decoration);
                }
                else
                {
                    aboveArticulations.Add(decoration);
                }
            }
        }

        // Position articulations above the note (use effective top boundary)
        if (aboveArticulations.Count > 0)
        {
            var firstArticulation = aboveArticulations.OrderBy(GetPlacementPriority).First();
            var firstOffset = GetArticulationOffset(firstArticulation);
            double currentY = effectiveTop - (firstOffset * context.StaffSpace);

            foreach (var decoration in aboveArticulations.OrderBy(GetPlacementPriority))
            {
                result.Add((decoration, noteX, currentY));
                currentY -= ArticulationStackSpacing * context.StaffSpace;
            }
        }

        // Position articulations below the note (use effective bottom boundary)
        if (belowArticulations.Count > 0)
        {
            var firstArticulation = belowArticulations.OrderBy(GetPlacementPriority).First();
            var firstOffset = GetArticulationOffset(firstArticulation);
            double currentY = effectiveBottom + (firstOffset * context.StaffSpace);

            foreach (var decoration in belowArticulations.OrderBy(GetPlacementPriority))
            {
                result.Add((decoration, noteX, currentY));
                currentY += ArticulationStackSpacing * context.StaffSpace;
            }
        }

        return result;
    }
}
