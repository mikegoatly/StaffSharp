namespace StaffSharp.Layout.Services;

using StaffSharp;
using StaffSharp.Layout;
using StaffSharp.Layout.Model;
using StaffSharp.Notation;

/// <summary>
/// Calculates stem directions, positions, and lengths for notes and chords.
/// </summary>
internal static class StemCalculator
{
    private const double StemLength = 3.5; // In staff spaces
    private const double MaxBeamSlopeInSpaces = 1.0; // Maximum beam slope in staff spaces

    /// <summary>
    /// Calculates stem properties for a single note or chord (not part of a beam group).
    /// </summary>
    public static void CalculateStem(
        IStemmedSymbol symbol,
        double staffBaseline,
        SvgContext context,
        bool? forceStemDirection = null)
    {
        // Determine stem direction based on average position (for chords) or notehead position (for single notes)
        var noteheadY = symbol.Y;
        var avgY = noteheadY;

        if (symbol is ChordLayoutSymbol chordSymbol && chordSymbol.NoteheadYPositions.Count > 0)
        {
            // For chords, use the average position to determine direction
            avgY = chordSymbol.NoteheadYPositions.Average();
        }

        bool stemUp = DetermineStemDirection(avgY, staffBaseline, symbol.VoiceNumber, forceStemDirection);

        // Get stem attachment point (outermost notehead for chords)
        var stemAttachmentY = GetStemAttachmentY(symbol, stemUp);

        // Calculate stem position
        var stemLength = StemLength * context.StaffSpace;

        // Stem X position: fixed offset from notehead
        // These match the rendering offsets: right edge for stem up, left edge for stem down
        var stemX = stemUp
            ? symbol.X + (context.StaffSpace + 1)  // Right edge for stem up
            : symbol.X + 1; // Left edge for stem down

        double stemY1 = stemAttachmentY;
        double stemY2 = stemUp
            ? stemAttachmentY - stemLength
            : stemAttachmentY + stemLength;

        // Set stem info
        symbol.Stem = new StemInfo(stemX, stemY1, stemY2, stemUp);
        symbol.Beam = BeamInfo.None;
    }

    /// <summary>
    /// Calculates stem properties for a group of beamed notes.
    /// </summary>
    public static void CalculateBeamedGroupStems(
        IReadOnlyList<LayoutSymbol> layoutGroup,
        double staffBaseline,
        SvgContext context)
    {
        if (layoutGroup.Count == 0) return;

        // Determine beam direction
        bool stemUp = DetermineBeamDirection(layoutGroup, staffBaseline);

        // Assign beam group IDs and calculate stem attachment points
        var beamGroupId = layoutGroup[0].GetHashCode();

        for (int i = 0; i < layoutGroup.Count; i++)
        {
            var symbol = layoutGroup[i];

            var beamCount = BeamGrouper.GetBeamCount(symbol);

            // Stem X position: fixed offset from notehead
            var stemX = stemUp
                ? symbol.X + (context.StaffSpace + 1)  // Right edge for stem up
                : symbol.X + 1; // Left edge for stem down

            // Calculate stem attachment point (where stem meets notehead)
            var noteheadY = symbol.Y;
            if (symbol is ChordLayoutSymbol chordSymbol && chordSymbol.NoteheadYPositions.Count > 0)
            {
                noteheadY = stemUp ? chordSymbol.NoteheadYPositions.Max() : chordSymbol.NoteheadYPositions.Min();
            }

            var stemY1 = noteheadY;

            if (symbol is not IStemmedSymbol stemmedSymbol)
            {
                throw new ArgumentException("Symbol must implement IStemmedSymbol", nameof(layoutGroup));
            }

            stemmedSymbol.Stem = new StemInfo(stemX, stemY1, stemY1, stemUp); // Y2 will be updated
            stemmedSymbol.Beam = new BeamInfo(beamGroupId, i == 0, i == layoutGroup.Count - 1, beamCount, false, 0);
        }

        // Calculate slanted beam position based on melodic contour
        CalculateBeamSlant(layoutGroup, context, stemUp);
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
        var firstSymbol = (IStemmedSymbol)group[0];
        var lastSymbol = (IStemmedSymbol)group[^1];

        // Get notehead Y positions for first and last notes
        var firstNoteheadY = firstSymbol.Stem.Y1; // Already set above
        var lastNoteheadY = lastSymbol.Stem.Y1;

        // Calculate beam endpoints based on standard stem length
        var stemLength = StemLength * context.StaffSpace;
        var beamY1 = stemUp ? firstNoteheadY - stemLength : firstNoteheadY + stemLength;
        var beamY2 = stemUp ? lastNoteheadY - stemLength : lastNoteheadY + stemLength;

        // Calculate beam slope based on stem X positions (not notehead centers)
        var beamSlope = (beamY2 - beamY1) / (lastSymbol.Stem.X - firstSymbol.Stem.X);

        // Limit beam slope to maximum angle (standard engraving practice)
        var maxSlopeInPixels = MaxBeamSlopeInSpaces * context.StaffSpace;
        var beamWidth = lastSymbol.Stem.X - firstSymbol.Stem.X;
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
        foreach (var symbol in group.Cast<IStemmedSymbol>())
        {
            var beamYAtThisNote = beamY1 + (symbol.Stem.X - firstSymbol.Stem.X) * beamSlope;
            var actualStemLength = Math.Abs(beamYAtThisNote - symbol.Stem.Y1);

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
            beamSlope = (beamY2 - beamY1) / (lastSymbol.Stem.X - firstSymbol.Stem.X);
        }

        // Set StemY2 for each note to meet the slanted beam at its stem X position
        foreach (var symbol in group.Cast<IStemmedSymbol>())
        {
            var beamYAtThisNote = beamY1 + (symbol.Stem.X - firstSymbol.Stem.X) * beamSlope;

            // Update the Stem with the new Y2 value
            symbol.Stem = symbol.Stem with { Y2 = beamYAtThisNote };
        }
    }

    private static double GetStemAttachmentY(IStemmedSymbol symbol, bool stemUp)
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
