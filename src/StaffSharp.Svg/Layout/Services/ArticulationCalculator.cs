namespace StaffSharp.Layout.Services;

using StaffSharp;
using StaffSharp.Layout.Model;
using StaffSharp.Notation;
using StaffSharp.Render;

/// <summary>
/// Calculates positions for articulations and decorations on notes and chords.
/// Follows standard music engraving practices.
/// </summary>
internal static class ArticulationCalculator
{
    private const double ArticulationStackSpacing = 0.15; // Staff spaces between stacked articulations

    /// <summary>
    /// Determines which side of the note an articulation should be placed.
    /// </summary>
    private enum ArticulationSide
    {
        AboveNote,        // Standard position above notehead
        OppositeToStem,   // On the side opposite the stem (default for most)
        AlwaysAboveStaff  // ALWAYS above top staff line (professional engraving)
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
            Decoration.Tenuto => 0,
            Decoration.Staccato => 1,

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
    /// Determines which side of the note the articulation should appear.
    /// </summary>
    private static ArticulationSide GetArticulationSide(Decoration decoration)
    {
        return decoration switch
        {
            Decoration.Fermata => ArticulationSide.AlwaysAboveStaff,
            Decoration.Breath => ArticulationSide.AlwaysAboveStaff,
            Decoration.Marcato => ArticulationSide.AlwaysAboveStaff,
            Decoration.UpBow => ArticulationSide.AlwaysAboveStaff,
            Decoration.DownBow => ArticulationSide.AlwaysAboveStaff,
            Decoration.Trill => ArticulationSide.AlwaysAboveStaff,

            // All other articulations go opposite to stem
            _ => ArticulationSide.OppositeToStem
        };
    }

    public static (double width, double height) GetDecorationGlyphSizeInStaffSpaces(Decoration decoration)
    {
        var glyph = GetDecorationGlyph(decoration);
        if (glyph == default)
        {
            return (0, 0);
        }
        
        double targetHeight = GetDecorationTargetHeight(decoration);
        double scale = glyph.GetScaleForHeight(targetHeight);
        double widthInStaffSpaces = glyph.Width * scale;

        return (widthInStaffSpaces, targetHeight);
    }

    /// <summary>
    /// Gets the target height in staff spaces for a decoration glyph.
    /// </summary>
    private static double GetDecorationTargetHeight(Decoration decoration)
    {
        return decoration switch
        {
            // Small articulations
            Decoration.Staccato => 0.4,

            // Medium articulations
            Decoration.Tenuto => 0.2,
            Decoration.Accent => 0.7,
            Decoration.Marcato => 0.7,
            Decoration.UpBow => 0.6,
            Decoration.DownBow => 0.6,

            // Large ornaments
            Decoration.Trill => 0.8,
            Decoration.Turn => 0.8,
            Decoration.UpperMordent => 0.8,
            Decoration.LowerMordent => 0.8,
            Decoration.Mordent => 0.8,
            Decoration.InvertedTurn => 0.8,

            // Fermata and breath marks
            Decoration.Fermata => 1.0,
            Decoration.Breath => 0.6,

            // Default for unspecified
            _ => 0.7
        };
    }

    /// <summary>
    /// Maps a Decoration enum to its corresponding SMuFL glyph.
    /// </summary>
    public static GlyphInfo GetDecorationGlyph(Decoration decoration)
    {
        return decoration switch
        {
            Decoration.Staccato => MusicGlyphs.Staccato,
            Decoration.Tenuto => MusicGlyphs.Tenuto,
            Decoration.Accent => MusicGlyphs.Accent,
            Decoration.Marcato => MusicGlyphs.Marcato,
            Decoration.Fermata => MusicGlyphs.Hold,
            Decoration.Breath => MusicGlyphs.BreathMark,
            Decoration.Trill => MusicGlyphs.Trill,
            Decoration.Turn => MusicGlyphs.Turn,
            Decoration.UpperMordent => MusicGlyphs.MordentUpper,
            Decoration.LowerMordent => MusicGlyphs.MordentLower,
            Decoration.UpBow => MusicGlyphs.Upbow,
            Decoration.DownBow => MusicGlyphs.Downbow,
            // Note: Some decorations don't have glyphs yet or aren't rendered as symbols
            _ => default
        };
    }

    /// <summary>
    /// Calculates articulation positions for a single note.
    /// </summary>
    /// <param name="symbol">The note symbol with stem and position information.</param>
    /// <param name="decorations">List of decorations to position.</param>
    /// <param name="context">SVG rendering context.</param>
    /// <param name="staffBaseline">Absolute Y coordinate of staff baseline (middle line).</param>
    /// <param name="staffTopY">Absolute Y coordinate of top staff line.</param>
    public static List<(Decoration Type, Bounds Bounds)> CalculateArticulations(
        IStemmedSymbol symbol,
        IReadOnlyList<Decoration> decorations,
        SvgContext context,
        double staffBaseline,
        double staffTopY)
    {
        if (decorations.Count == 0)
        {
            return [];
        }

        var sortedDecorations = decorations
            .Where(d => GetPlacementPriority(d) < 99)
            .OrderBy(GetPlacementPriority)
            .ToList();

        if (sortedDecorations.Count == 0)
        {
            return [];
        }

        var noteheadBounds = symbol.NoteHeadBounds;
        var noteCenter = noteheadBounds.X + (noteheadBounds.Width / 2.0);
        bool stemUp = symbol.Stem.Up;

        // Split into Above/Below stacks
        var aboveList = new List<Decoration>();
        var belowList = new List<Decoration>();

        foreach (var deco in sortedDecorations)
        {
            var side = GetArticulationSide(deco);

            if (side == ArticulationSide.AlwaysAboveStaff || side == ArticulationSide.AboveNote)
            {
                aboveList.Add(deco);
            }
            else
            {
                if (stemUp)
                {
                    belowList.Add(deco);
                }
                else
                {
                    aboveList.Add(deco);
                }
            }
        }

        var result = new List<(Decoration Type, Bounds Bounds)>();
        if (aboveList.Count > 0)
        {
            // Calculate Anchor:
            // If the first item is "Always Above" (like a Bow), we anchor to the Staff Top.
            // UNLESS the note is extremely high, in which case we anchor to the note.
            // If it's a relative item (Staccato on down-stem), we anchor to the note/stem top.
            double anchorY;
            bool isFixedHigh = GetArticulationSide(aboveList[0]) == ArticulationSide.AlwaysAboveStaff;
            double visualTop = stemUp ? symbol.Stem.Y2 : noteheadBounds.Y; // Stem Tip or Note Top

            if (isFixedHigh)
            {
                // Ensure we don't go below the staff top, but also don't collide with a high note
                anchorY = Math.Min(staffTopY, visualTop);
            }
            else
            {
                anchorY = visualTop;
            }

            result.AddRange(LayoutStack(
                aboveList,
                context,
                staffBaseline,
                anchorY,
                noteCenter,
                noteheadBounds,
                isStackAbove: true));
        }

        if (belowList.Count > 0)
        {
            // Anchor is usually just the bottom of the notehead (Stem is Up)
            double startY = noteheadBounds.Y2;

            result.AddRange(LayoutStack(
                belowList,
                context,
                staffBaseline,
                startY,
                noteCenter,
                noteheadBounds,
                isStackAbove: false));
        }

        return result;
    }

    private static List<(Decoration, Bounds)> LayoutStack(
        List<Decoration> items,
        SvgContext context,
        double staffBaseline,
        double startY,
        double noteCenterX,
        Bounds noteBounds,
        bool isStackAbove)
    {
        var result = new List<(Decoration, Bounds)>(items.Count);

        // Current Y position cursor
        double currentY = startY;
        foreach (var decoration in items)
        {
            var (baseWidth, baseHeight) = GetDecorationGlyphSizeInStaffSpaces(decoration);
            var bounds = new Bounds
            {
                Width = baseWidth * context.StaffSpace,
                Height = baseHeight * context.StaffSpace
            };

            double placementY;
            if (isStackAbove)
            {
                // Move cursor UP (negative) by the gap
                currentY -= ArticulationStackSpacing * context.StaffSpace;

                // The top of the symbol is at (Cursor - Height)
                placementY = currentY - bounds.Height;
            }
            else
            {
                // Move cursor DOWN (positive) by the gap
                currentY += ArticulationStackSpacing * context.StaffSpace;
                placementY = currentY;
            }

            // Snap to Space (Collision Avoidance)
            // Only strictly required for small items inside the staff
            if (decoration == Decoration.Staccato || decoration == Decoration.Tenuto)
            {
                // This function returns a corrected Y
                double snappedY = AdjustForStaffLineCollision(placementY, bounds.Height, staffBaseline, context, isStackAbove);

                // If we snapped, we need to update our 'currentY' cursor so the NEXT item 
                // stacks on top of the corrected position, not the original calculated position.
                if (Math.Abs(snappedY - placementY) > 0.001)
                {
                    placementY = snappedY;

                    // Re-sync the cursor to this new edge
                    if (isStackAbove)
                    {
                        currentY = placementY;
                    }
                    else
                    {
                        currentY = placementY + bounds.Height;
                    }
                }
            }

            // Determine X Position
            double finalX;
            if (decoration == Decoration.Breath)
            {
                // Breath marks: Right of the note
                finalX = noteBounds.X + noteBounds.Width + context.HalfStaffSpace;
            }
            else
            {
                // Standard: Centered
                finalX = noteCenterX - (bounds.Width * 0.5);
            }

            result.Add((decoration, bounds.Offset(finalX, placementY)));

            // Advance Cursor for next item
            currentY = isStackAbove ? placementY : placementY + bounds.Height;
        }

        return result;
    }

    private static double AdjustForStaffLineCollision(
           double topY,
           double glyphHeight,
           double staffBaseline,
           SvgContext context,
           bool isMovingUp)
    {
        double glyphCenter = topY + (glyphHeight * 0.5);

        // Convert Center Y to Staff Position (0 = middle line, 1 = space above, 2 = line above...)
        double relativeY = staffBaseline - glyphCenter;
        double staffPosition = relativeY / context.HalfStaffSpace;

        // Check if roughly on a line (tolerance 0.25 staff steps)
        double roundedPosition = Math.Round(staffPosition);
        bool isOnLine = Math.Abs(roundedPosition % 2) < 0.1; // Even numbers are lines
        bool isCloseToLine = Math.Abs(staffPosition - roundedPosition) < 0.25;

        // Only adjust if we are 'inside' or touching the staff (lines -4 to +4)
        // We allow snapping just outside (e.g. sitting on top line) to push into the space above.
        bool needsAdjustment = isOnLine && isCloseToLine && Math.Abs(roundedPosition) <= 4.0;

        if (needsAdjustment)
        {
            // Push away in the direction of the stack
            // If moving Up, we want a higher position index (e.g. 4 -> 5)
            double shift = isMovingUp ? 1.0 : -1.0;
            double targetPosition = roundedPosition + shift;

            // Convert Target Center Position back to Top-Left Y
            // targetRelY = targetPosition * halfSpace
            // targetCenterY = baseline - targetRelY
            // targetTopY = targetCenterY - halfHeight

            double targetRelY = targetPosition * context.HalfStaffSpace;
            double targetCenterY = staffBaseline - targetRelY;

            return targetCenterY - (glyphHeight * 0.5);
        }

        return topY;
    }
}
