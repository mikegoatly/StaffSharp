namespace StaffSharp.Svg.Layout.Services;

using StaffSharp.Notation;

/// <summary>
/// Calculates stem directions, positions, and lengths for notes and chords.
/// </summary>
public static class StemCalculator
{
    private const double StemLength = 3.5; // In staff spaces
    private const double MaxBeamSlopeInSpaces = 1.0; // Maximum beam slope in staff spaces

    /// <summary>
    /// Calculates stem properties for a single note or chord (not part of a beam group).
    /// </summary>
    public static void CalculateStem(
        LayoutSymbol symbol,
        double staffBaseline,
        SvgContext context,
        bool? forceStemDirection = null)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        ArgumentNullException.ThrowIfNull(context);

        // Determine stem direction based on average position (for chords) or notehead position (for single notes)
        var noteheadY = symbol.Y;
        var avgY = noteheadY;

        if (symbol is ChordLayoutSymbol chordSymbol && chordSymbol.NoteheadYPositions.Count > 0)
        {
            // For chords, use the average position to determine direction
            avgY = chordSymbol.NoteheadYPositions.Average();
        }

        bool stemUp = DetermineStemDirection(avgY, staffBaseline, symbol.VoiceNumber, forceStemDirection);
        symbol.StemUp = stemUp;

        // Get stem attachment point (outermost notehead for chords)
        var stemAttachmentY = GetStemAttachmentY(symbol, stemUp);

        // Calculate stem position
        var stemLength = StemLength * context.StaffSpace;

        // Stem X position: fixed offset from notehead
        // These match the rendering offsets: right edge for stem up, left edge for stem down
        symbol.StemX = stemUp
            ? symbol.X + (context.StaffSpace + 1)  // Right edge for stem up
            : symbol.X + 1; // Left edge for stem down

        if (stemUp)
        {
            symbol.StemY1 = stemAttachmentY;
            symbol.StemY2 = stemAttachmentY - stemLength;
        }
        else
        {
            symbol.StemY1 = stemAttachmentY;
            symbol.StemY2 = stemAttachmentY + stemLength;
        }
    }

    /// <summary>
    /// Calculates stem properties for a group of beamed notes.
    /// </summary>
    public static void CalculateBeamedGroupStems(
        IReadOnlyList<LayoutSymbol> group,
        double staffBaseline,
        SvgContext context)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(context);

        if (group.Count == 0) return;

        // Determine beam direction
        bool stemUp = DetermineBeamDirection(group, staffBaseline);

        // Assign beam group IDs and calculate stem attachment points
        var beamGroupId = group[0].GetHashCode();

        for (int i = 0; i < group.Count; i++)
        {
            var symbol = group[i];
            symbol.BeamGroupId = beamGroupId;
            symbol.IsFirstInBeamGroup = (i == 0);
            symbol.IsLastInBeamGroup = (i == group.Count - 1);
            symbol.BeamCount = BeamGrouper.GetBeamCount(symbol);
            symbol.StemUp = stemUp;

            // Stem X position: fixed offset from notehead
            symbol.StemX = stemUp
                ? symbol.X + (context.StaffSpace + 1)  // Right edge for stem up
                : symbol.X + 1; // Left edge for stem down

            // Calculate stem attachment point (where stem meets notehead)
            var noteheadY = symbol.Y;
            if (symbol is ChordLayoutSymbol chordSymbol && chordSymbol.NoteheadYPositions.Count > 0)
            {
                noteheadY = stemUp ? chordSymbol.NoteheadYPositions.Max() : chordSymbol.NoteheadYPositions.Min();
            }
            symbol.StemY1 = noteheadY;
        }

        // Calculate slanted beam position based on melodic contour
        CalculateBeamSlant(group, context, stemUp);
    }

    /// <summary>
    /// Determines if a symbol requires a stem (all notes except whole notes).
    /// </summary>
    public static bool RequiresStem(LayoutSymbol symbol)
    {
        SymbolicDuration? duration = symbol switch
        {
            NoteLayoutSymbol noteSymbol => noteSymbol.Note.Duration,
            ChordLayoutSymbol chordSymbol => chordSymbol.Chord.Duration,
            _ => null
        };

        if (!duration.HasValue) return false;

        // Whole notes don't have stems
        return duration.Value.Base != NoteDurationBase.Whole;
    }

    private static bool DetermineStemDirection(
        double noteheadY,
        double staffBaseline,
        int voiceNumber,
        bool? forcedDirection)
    {
        // Stem direction logic:
        // 1. If forced (for beam groups), use that
        // 2. If multi-voice (voice > 1), use voice-based direction (voice 1 up, voice 2+ down)
        // 3. Otherwise, based on notehead position relative to staff center
        if (forcedDirection.HasValue)
        {
            return forcedDirection.Value;
        }
        else if (voiceNumber > 1)
        {
            // In multi-voice: voice 1 stems up, voice 2+ stems down
            return voiceNumber == 1;
        }
        else
        {
            // Single voice: based on position relative to middle line
            // In SVG coordinates, Y increases downward
            // Notes below middle line (higher Y) → stems up
            // Notes above middle line (lower Y) → stems down
            return noteheadY > staffBaseline;
        }
    }

    private static bool DetermineBeamDirection(IReadOnlyList<LayoutSymbol> group, double staffBaseline)
    {
        // Determine beam direction:
        // 1. If all notes are same voice > 0, use voice-based direction
        // 2. Otherwise, use average position
        var firstVoice = group[0].VoiceNumber;
        var allSameVoice = group.All(s => s.VoiceNumber == firstVoice) && firstVoice > 0;

        bool stemUp;
        if (allSameVoice && firstVoice > 1)
        {
            // Voice 2+ in multi-voice: stems down
            stemUp = false;
        }
        else if (allSameVoice && firstVoice == 1 && group.Any(s => s.VoiceNumber != 1))
        {
            // Voice 1 when there are other voices: stems up
            stemUp = true;
        }
        else
        {
            // Calculate average Y position to determine beam direction
            var avgY = 0.0;
            foreach (var symbol in group)
            {
                if (symbol is NoteLayoutSymbol noteSymbol)
                {
                    avgY += noteSymbol.Y;
                }
                else if (symbol is ChordLayoutSymbol chordSymbol && chordSymbol.NoteheadYPositions.Count > 0)
                {
                    avgY += chordSymbol.NoteheadYPositions.Average();
                }
            }
            avgY /= group.Count;

            // In SVG coordinates, Y increases downward
            // Notes below middle line (higher Y) → stems up
            // Notes above middle line (lower Y) → stems down
            stemUp = avgY > staffBaseline;
        }

        return stemUp;
    }

    private static void CalculateBeamSlant(IReadOnlyList<LayoutSymbol> group, SvgContext context, bool stemUp)
    {
        var firstSymbol = group[0];
        var lastSymbol = group[^1];

        // Get notehead Y positions for first and last notes
        var firstNoteheadY = firstSymbol.StemY1; // Already set above
        var lastNoteheadY = lastSymbol.StemY1;

        // Calculate beam endpoints based on standard stem length
        var stemLength = StemLength * context.StaffSpace;
        var beamY1 = stemUp ? firstNoteheadY - stemLength : firstNoteheadY + stemLength;
        var beamY2 = stemUp ? lastNoteheadY - stemLength : lastNoteheadY + stemLength;

        // Calculate beam slope based on stem X positions (not notehead centers)
        var beamSlope = (beamY2 - beamY1) / (lastSymbol.StemX - firstSymbol.StemX);

        // Limit beam slope to maximum angle (standard engraving practice)
        var maxSlopeInPixels = MaxBeamSlopeInSpaces * context.StaffSpace;
        var beamWidth = lastSymbol.StemX - firstSymbol.StemX;
        var maxSlope = maxSlopeInPixels / beamWidth;

        if (Math.Abs(beamSlope) > maxSlope)
        {
            // Limit the slope and adjust beam endpoints symmetrically
            var limitedSlope = Math.Sign(beamSlope) * maxSlope;
            var beamMidY = (beamY1 + beamY2) / 2;
            var halfWidth = beamWidth / 2;

            beamY1 = beamMidY - (limitedSlope * halfWidth);
            beamY2 = beamMidY + (limitedSlope * halfWidth);
            beamSlope = limitedSlope;
        }

        // Check if all notes meet minimum stem length with this beam position
        // Find the maximum shortfall across all notes
        var maxShortfall = 0.0;
        foreach (var symbol in group)
        {
            var beamYAtThisNote = beamY1 + (symbol.StemX - firstSymbol.StemX) * beamSlope;
            var actualStemLength = Math.Abs(beamYAtThisNote - symbol.StemY1);

            if (actualStemLength < stemLength)
            {
                var shortfall = stemLength - actualStemLength;
                maxShortfall = Math.Max(maxShortfall, shortfall);
            }
        }

        // If any note has insufficient stem length, shift the entire beam
        if (maxShortfall > 0)
        {
            if (stemUp)
            {
                // Beam is above noteheads, shift it up (decrease Y)
                beamY1 -= maxShortfall;
                beamY2 -= maxShortfall;
            }
            else
            {
                // Beam is below noteheads, shift it down (increase Y)
                beamY1 += maxShortfall;
                beamY2 += maxShortfall;
            }

            // Recalculate slope with adjusted beam position
            beamSlope = (beamY2 - beamY1) / (lastSymbol.StemX - firstSymbol.StemX);
        }

        // Set StemY2 for each note to meet the slanted beam at its stem X position
        foreach (var symbol in group)
        {
            var beamYAtThisNote = beamY1 + (symbol.StemX - firstSymbol.StemX) * beamSlope;
            symbol.StemY2 = beamYAtThisNote;
        }
    }

    private static double GetStemAttachmentY(LayoutSymbol symbol, bool stemUp)
    {
        // For single notes, use the notehead Y position
        // For chords, use the outermost notehead (bottom note for stem up, top note for stem down)
        if (symbol is ChordLayoutSymbol chordSymbol && chordSymbol.NoteheadYPositions.Count > 0)
        {
            return stemUp
                ? chordSymbol.NoteheadYPositions.Max()  // Bottom note (highest Y) for stem up
                : chordSymbol.NoteheadYPositions.Min(); // Top note (lowest Y) for stem down
        }

        return symbol.Y;
    }
}
