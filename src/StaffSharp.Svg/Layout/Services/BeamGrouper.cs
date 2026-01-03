namespace StaffSharp.Svg.Layout.Services;

using System.Collections.ObjectModel;
using StaffSharp.Notation;

/// <summary>
/// Groups notes into beam groups based on note durations and beat positions.
/// </summary>
public static class BeamGrouper
{
    /// <summary>
    /// Groups beamable notes in a measure, respecting voice boundaries and beat structure.
    /// </summary>
    /// <param name="symbols">The symbols in the measure.</param>
    /// <param name="timeSignature">The time signature of the measure (optional - if provided, enables beat-aware grouping).</param>
    /// <returns>A list of beam groups, where each group contains consecutive beamable notes.</returns>
    public static IReadOnlyList<IReadOnlyList<LayoutSymbol>> GroupBeamableNotes(
        IEnumerable<LayoutSymbol> symbols,
        TimeSignature? timeSignature = null)
    {
        ArgumentNullException.ThrowIfNull(symbols);

        // If no time signature provided, use simple voice-based grouping
        if (timeSignature == null)
        {
            return GroupByVoice(symbols);
        }

        // Use beat-aware grouping
        return GroupByBeats(symbols, timeSignature);
    }

    /// <summary>
    /// Groups beamable notes by voice only (no beat awareness).
    /// </summary>
    private static ReadOnlyCollection<List<LayoutSymbol>> GroupByVoice(IEnumerable<LayoutSymbol> symbols)
    {
        var beamGroups = new List<List<LayoutSymbol>>();
        var currentGroup = new List<LayoutSymbol>();
        int currentVoice = -1;

        foreach (var symbol in symbols)
        {
            if (IsBeamable(symbol))
            {
                // Start new group if voice changes
                if (symbol.VoiceNumber != currentVoice && currentGroup.Count > 0)
                {
                    beamGroups.Add(currentGroup);
                    currentGroup = new List<LayoutSymbol>();
                }
                currentGroup.Add(symbol);
                currentVoice = symbol.VoiceNumber;
            }
            else
            {
                if (currentGroup.Count > 0)
                {
                    beamGroups.Add(currentGroup);
                    currentGroup = new List<LayoutSymbol>();
                    currentVoice = -1;
                }
            }
        }

        if (currentGroup.Count > 0)
        {
            beamGroups.Add(currentGroup);
        }

        return beamGroups.AsReadOnly();
    }

    /// <summary>
    /// Groups beamable notes respecting beat boundaries based on time signature.
    /// </summary>
    private static ReadOnlyCollection<List<LayoutSymbol>> GroupByBeats(
        IEnumerable<LayoutSymbol> symbols,
        TimeSignature timeSignature)
    {
        var beamGroups = new List<List<LayoutSymbol>>();
        var currentGroup = new List<LayoutSymbol>();
        var currentPosition = Rational.Zero;
        var currentBeatStart = Rational.Zero;
        int currentVoice = -1;

        // Calculate beat duration based on time signature
        var beatDuration = GetBeatDuration(timeSignature);

        foreach (var symbol in symbols)
        {
            var duration = GetSymbolDuration(symbol);

            // Skip symbols without duration
            if (!duration.HasValue)
            {
                // End current group for unknown symbols
                if (currentGroup.Count > 0)
                {
                    beamGroups.Add(currentGroup);
                    currentGroup = new List<LayoutSymbol>();
                    currentVoice = -1;
                }
                continue;
            }

            if (IsBeamable(symbol))
            {
                // Calculate which beat this note starts on
                var beatNumberDouble = currentPosition.ToDouble() / beatDuration.ToDouble();
                var beatNumber = (int)beatNumberDouble;
                var thisBeatStart = beatDuration * Rational.Create(beatNumber, 1);

                // Start new group if:
                // 1. Voice changed
                // 2. We crossed a beat boundary
                bool shouldBreak = (symbol.VoiceNumber != currentVoice && currentGroup.Count > 0) ||
                                   (currentGroup.Count > 0 && thisBeatStart != currentBeatStart);

                if (shouldBreak)
                {
                    beamGroups.Add(currentGroup);
                    currentGroup = new List<LayoutSymbol>();
                }

                currentGroup.Add(symbol);
                currentVoice = symbol.VoiceNumber;
                currentBeatStart = thisBeatStart;
            }
            else
            {
                // Non-beamable note - end current group
                if (currentGroup.Count > 0)
                {
                    beamGroups.Add(currentGroup);
                    currentGroup = new List<LayoutSymbol>();
                    currentVoice = -1;
                }
            }

            currentPosition += duration.Value;
        }

        if (currentGroup.Count > 0)
        {
            beamGroups.Add(currentGroup);
        }

        return beamGroups.AsReadOnly();
    }

    /// <summary>
    /// Determines the beat duration for beam grouping based on time signature.
    /// Returns the duration in quarter note beats.
    /// For beaming purposes, we use larger groupings than individual beats:
    /// - 4/4: half note (2 quarter notes)
    /// - 3/4: quarter note (1 quarter note)
    /// - 6/8: dotted quarter (3 eighths)
    /// </summary>
    private static Rational GetBeatDuration(TimeSignature timeSignature)
    {
        // For compound meters (numerator divisible by 3 and > 3), beat is dotted quarter
        if (timeSignature.Numerator % 3 == 0 && timeSignature.Numerator > 3)
        {
            // Compound meter: 6/8, 9/8, 12/8, etc.
            // Beat = dotted quarter (3 eighth notes) = 1.5 quarter notes
            return Rational.Create(3 * 4, timeSignature.Denominator);
        }
        // Special case for 4/4 and 2/2: beam in half-note groups
        else if ((timeSignature.Numerator == 4 && timeSignature.Denominator == 4) ||
                 (timeSignature.Numerator == 2 && timeSignature.Denominator == 2))
        {
            // 4/4 or 2/2: group by half notes (2 quarter notes)
            return Rational.Create(2, 1);
        }
        else
        {
            // Other simple meters (3/4, 2/4, 3/8, etc.): group by quarter note beat
            // For 3/4: 1 quarter note per beat
            // For 2/4: 1 quarter note per beat
            // For 3/8: 1/2 quarter note per beat (denominator = 8)
            return Rational.Create(4, timeSignature.Denominator);
        }
    }

    /// <summary>
    /// Gets the duration of a symbol as a Rational value in quarter note beats.
    /// </summary>
    private static Rational? GetSymbolDuration(LayoutSymbol symbol)
    {
        SymbolicDuration? duration = symbol switch
        {
            NoteLayoutSymbol noteSymbol => noteSymbol.Note.Duration,
            ChordLayoutSymbol chordSymbol => chordSymbol.Chord.Duration,
            RestLayoutSymbol restSymbol => restSymbol.Rest.Duration,
            _ => null
        };

        return duration?.ToBeats();
    }

    /// <summary>
    /// Determines if a symbol can be beamed (eighth notes and shorter).
    /// </summary>
    public static bool IsBeamable(LayoutSymbol symbol)
    {
        SymbolicDuration? duration = symbol switch
        {
            NoteLayoutSymbol noteSymbol => noteSymbol.Note.Duration,
            ChordLayoutSymbol chordSymbol => chordSymbol.Chord.Duration,
            _ => null
        };

        if (!duration.HasValue) return false;

        // Eighth notes and shorter can be beamed
        return duration.Value.Base >= NoteDurationBase.Eighth;
    }

    /// <summary>
    /// Gets the number of beams needed for a note based on its duration.
    /// </summary>
    public static int GetBeamCount(LayoutSymbol symbol)
    {
        SymbolicDuration? duration = symbol switch
        {
            NoteLayoutSymbol noteSymbol => noteSymbol.Note.Duration,
            ChordLayoutSymbol chordSymbol => chordSymbol.Chord.Duration,
            _ => null
        };

        if (!duration.HasValue) return 0;

        return duration.Value.Base switch
        {
            NoteDurationBase.Eighth => 1,
            NoteDurationBase.Sixteenth => 2,
            NoteDurationBase.ThirtySecond => 3,
            _ => 0
        };
    }
}
