namespace StaffSharp.Svg.Layout.Passes;

using StaffSharp.Notation;
using StaffSharp.Svg;

/// <summary>
/// Calculates positions for augmentation dots on notes, rests, and chords.
/// Must run after HorizontalPositionPass since it needs final X positions.
/// </summary>
public class DotPositioningPass : ILayoutPass
{
    private const double DotSpacing = 0.25; // Space between multiple dots (in staff spaces)
    private const double DotOffset = 0.2;   // Offset from note head to first dot (in staff spaces)

    public void Run(LayoutModel model, SvgContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var system in model.Systems)
        {
            foreach (var staff in system.Staves)
            {
                foreach (var measure in staff.Measures)
                {
                    ProcessMeasure(measure, context);
                }
            }
        }
    }

    private static void ProcessMeasure(LayoutMeasure measure, SvgContext context)
    {
        foreach (var symbol in measure.Symbols)
        {
            switch (symbol)
            {
                case NoteLayoutSymbol noteSymbol when noteSymbol.DotCount > 0:
                    PositionNoteDots(noteSymbol, context);
                    break;

                case RestLayoutSymbol restSymbol when restSymbol.DotCount > 0:
                    PositionRestDots(restSymbol, context);
                    break;

                case ChordLayoutSymbol chordSymbol when chordSymbol.DotCount > 0:
                    PositionChordDots(chordSymbol, context);
                    break;
            }
        }
    }

    private static void PositionNoteDots(NoteLayoutSymbol noteSymbol, SvgContext context)
    {
        // Calculate X positions for each dot
        var noteHeadWidth = GetNoteHeadWidth(noteSymbol.Note.Duration.Base, context);
        var baseX = noteSymbol.X + noteHeadWidth + (DotOffset * context.StaffSpace);

        // Y position: if note is on a line, move dot to the space above
        var dotY = IsOnStaffLine(noteSymbol.Y, context)
            ? noteSymbol.Y - (context.StaffSpace * 0.5)
            : noteSymbol.Y;

        PositionAugmentationDots(noteSymbol, context, baseX, dotY);
    }

    private static void PositionRestDots(RestLayoutSymbol restSymbol, SvgContext context)
    {
        // Calculate X positions for each dot
        var restWidth = GetRestWidth(restSymbol.Rest.Duration.Base, context);
        var baseX = restSymbol.X + restWidth + (DotOffset * context.StaffSpace);

        // Add a little padding to the right of the rest for dots - the "bar" rests are wider
        baseX += 0.1 * context.StaffSpace;

        // Rest dots typically go in the third space (from bottom)
        // Y position is already set for the rest itself, use same position
        PositionAugmentationDots(restSymbol, context, baseX, restSymbol.Y);
    }

    private static void PositionAugmentationDots(AugmentationDottedLayoutSymbol symbol, SvgContext context, double baseX, double dotY)
    {
        for (int i = 0; i < symbol.DotCount; i++)
        {
            symbol.DotXPositions.Add(baseX + (i * DotSpacing * context.StaffSpace));
        }

        symbol.DotY = dotY;
    }

    private static void PositionChordDots(ChordLayoutSymbol chordSymbol, SvgContext context)
    {
        // Calculate X positions for each dot
        var noteHeadWidth = GetNoteHeadWidth(chordSymbol.Chord.Duration.Base, context);

        // Find the maximum X shift from any notehead to account for chord notehead shifts
        var maxXShift = chordSymbol.NoteheadXShifts.Count > 0
            ? chordSymbol.NoteheadXShifts.Max()
            : 0.0;

        var baseX = chordSymbol.X + noteHeadWidth + maxXShift + (DotOffset * context.StaffSpace);

        // For chords, find the topmost note and position dots relative to it
        // If topmost note is on a line, move dot to space above
        double dotY;
        if (chordSymbol.NoteheadYPositions.Count > 0)
        {
            var topNoteY = chordSymbol.NoteheadYPositions.Min(); // Min Y is topmost note
            dotY = IsOnStaffLine(topNoteY, context)
                ? topNoteY - (context.StaffSpace * 0.5)
                : topNoteY;
        }
        else
        {
            dotY = chordSymbol.Y;
        }

        PositionAugmentationDots(chordSymbol, context, baseX, dotY);
    }

    private static double GetNoteHeadWidth(NoteDurationBase durationBase, SvgContext context)
    {
        // Note head width is approximately 1.4 staff spaces (based on glyph design)
        return 1.4 * context.StaffSpace;
    }

    private static double GetRestWidth(NoteDurationBase durationBase, SvgContext context)
    {
        // Rest widths vary by type, but a reasonable approximation is 1.0 staff spaces
        return 1.0 * context.StaffSpace;
    }

    private static bool IsOnStaffLine(double y, SvgContext context)
    {
        // Staff lines are at Y positions that are multiples of StaffSpace
        // Check if Y is close to a multiple of StaffSpace
        var lineNumber = Math.Round(y / context.StaffSpace);
        var distanceToLine = Math.Abs(y - (lineNumber * context.StaffSpace));

        // Consider "on a line" if within 10% of staff space from the line
        return distanceToLine < (0.1 * context.StaffSpace);
    }
}
