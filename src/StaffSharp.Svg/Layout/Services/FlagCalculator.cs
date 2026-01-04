namespace StaffSharp.Svg.Layout.Services;

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
    public static void CalculateFlag(LayoutSymbol symbol, SvgContext context)
    {
        // Get the duration from the symbol
        var duration = GetDuration(symbol);
        if (!duration.HasValue)
        {
            return;
        }

        // Only apply flags to eighth notes and shorter that are NOT in a beam group
        if (duration.Value.Base >= NoteDurationBase.Eighth && !symbol.BeamGroupId.HasValue)
        {
            symbol.RequiresFlag = true;
            symbol.FlagCount = GetFlagCount(duration.Value.Base);
        }
        else
        {
            symbol.RequiresFlag = false;
            symbol.FlagCount = 0;
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
