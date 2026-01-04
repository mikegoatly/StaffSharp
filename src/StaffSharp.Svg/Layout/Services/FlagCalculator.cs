namespace StaffSharp.Svg.Layout.Services;

using StaffSharp.Layout.Model;
using StaffSharp.Notation;

/// <summary>
/// Calculates flag requirements for notes and chords.
/// </summary>
internal static class FlagCalculator
{
    /// <summary>
    /// Calculates whether a symbol requires flags and how many.
    /// Flags are needed for eighth notes and shorter that are not part of a beam group.
    /// </summary>
    public static void CalculateFlag(IStemmedSymbol stemmedSymbol, SvgContext context)
    {
        var symbol = (LayoutSymbol)stemmedSymbol;

        // Get the duration from the symbol
        var duration = GetDuration(symbol);
        if (!duration.HasValue)
        {
            return;
        }

        // Only apply flags to eighth notes and shorter that are NOT in a beam group
        if (duration.Value.Base >= NoteDurationBase.Eighth && !stemmedSymbol.Beam.IsBeamed)
        {
            var flagCount = GetFlagCount(duration.Value.Base);
            var currentStem = stemmedSymbol.Stem;
            var currentBeam = stemmedSymbol.Beam;

            // Update beam info with flag requirements
            var newBeam = new BeamInfo(
                currentBeam.GroupId,
                currentBeam.IsFirstInGroup,
                currentBeam.IsLastInGroup,
                currentBeam.BeamCount,
                true, // RequiresFlag
                flagCount
            );

            if (stemmedSymbol is NoteLayoutSymbol note)
            {
                note.Beam = newBeam;
            }
            else if (stemmedSymbol is ChordLayoutSymbol chord)
            {
                chord.Beam = newBeam;
            }
        }
    }

    /// <summary>
    /// Determines if a symbol requires a stem (and potentially flags).
    /// </summary>
    public static bool RequiresStem(LayoutSymbol symbol)
    {
        var duration = GetDuration(symbol);
        return duration.HasValue && duration.Value.Base != NoteDurationBase.Whole;
    }

    private static SymbolicDuration? GetDuration(LayoutSymbol symbol)
    {
        return symbol switch
        {
            // TODO Move this to an interface implemented by NoteLayoutSymbol and ChordLayoutSymbol?
            NoteLayoutSymbol noteSymbol => noteSymbol.Note.Duration,
            ChordLayoutSymbol chordSymbol => chordSymbol.Chord.Duration,
            RestLayoutSymbol => null, // Rests don't have flags
            _ => null
        };
    }

    private static int GetFlagCount(NoteDurationBase duration)
    {
        return duration switch
        {
            NoteDurationBase.Eighth => 1,
            NoteDurationBase.Sixteenth => 2,
            NoteDurationBase.ThirtySecond => 3,
            _ => 0
        };
    }
}
