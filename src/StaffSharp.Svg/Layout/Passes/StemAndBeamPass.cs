namespace StaffSharp.Svg.Layout.Passes;

using StaffSharp.Notation;
using StaffSharp.Svg;

/// <summary>
/// Calculates stem directions, lengths, and beam positions for notes.
/// </summary>
public class StemAndBeamPass : ILayoutPass
{
    private const double StemLength = 3.5; // In staff spaces

    public void Run(LayoutModel model, SvgContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var system in model.Systems)
        {
            foreach (var staff in system.Staves)
            {
                // Staff baseline (middle line)
                var staffBaseline = staff.Y + (2 * context.StaffSpace);

                foreach (var measure in staff.Measures)
                {
                    ProcessMeasure(measure, staffBaseline, context);
                }
            }
        }
    }

    private static void ProcessMeasure(LayoutMeasure measure, double staffBaseline, SvgContext context)
    {
        // Group beamable notes together, but separate by voice
        var beamGroups = new List<List<LayoutSymbol>>();
        var currentGroup = new List<LayoutSymbol>();
        int currentVoice = -1;

        foreach (var symbol in measure.Symbols)
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

                // Process single notes/chords with stems
                if (RequiresStem(symbol))
                {
                    CalculateStem(symbol, staffBaseline, context);
                }
            }
        }

        if (currentGroup.Count > 0)
        {
            beamGroups.Add(currentGroup);
        }

        // Process beam groups
        foreach (var group in beamGroups)
        {
            if (group.Count > 1)
            {
                CalculateBeamedGroup(group, staffBaseline, context);
            }
            else if (group.Count == 1)
            {
                CalculateStem(group[0], staffBaseline, context);
            }
        }
    }

    private static bool IsBeamable(LayoutSymbol symbol)
    {
        SymbolicDuration? duration = symbol switch
        {
            NoteLayoutSymbol noteSymbol => noteSymbol.Note.Duration,
            ChordLayoutSymbol chordSymbol => chordSymbol.Chord.Duration,
            _ => (SymbolicDuration?)null
        };

        if (!duration.HasValue) return false;

        // Eighth notes and shorter can be beamed
        return duration.Value.Base >= NoteDurationBase.Eighth;
    }

    private static bool RequiresStem(LayoutSymbol symbol)
    {
        SymbolicDuration? duration = symbol switch
        {
            NoteLayoutSymbol noteSymbol => noteSymbol.Note.Duration,
            ChordLayoutSymbol chordSymbol => chordSymbol.Chord.Duration,
            _ => (SymbolicDuration?)null
        };

        if (!duration.HasValue) return false;

        // Whole notes don't have stems
        return duration.Value.Base != NoteDurationBase.Whole;
    }

    private static void CalculateStem(LayoutSymbol symbol, double staffBaseline, SvgContext context, bool? forceStemDirection = null)
    {
        // Determine stem direction:
        // 1. If forced (for beam groups), use that
        // 2. If multi-voice (voice > 1), use voice-based direction (voice 1 up, voice 2+ down)
        // 3. Otherwise, based on notehead position relative to staff center
        var noteheadY = symbol.Y;

        if (symbol is ChordLayoutSymbol chordSymbol && chordSymbol.NoteheadYPositions.Count > 0)
        {
            // For chords, use the average position
            noteheadY = chordSymbol.NoteheadYPositions.Average();
        }

        bool stemUp;
        if (forceStemDirection.HasValue)
        {
            stemUp = forceStemDirection.Value;
        }
        else if (symbol.VoiceNumber > 1)
        {
            // In multi-voice: voice 1 stems up, voice 2+ stems down
            stemUp = symbol.VoiceNumber == 1;
        }
        else
        {
            // Single voice: based on position relative to middle line
            stemUp = noteheadY < staffBaseline;
        }

        symbol.StemUp = stemUp;

        // Calculate stem position
        var stemLength = StemLength * context.StaffSpace;
        symbol.StemX = symbol.X; // Stems attach to the right side of noteheads for up stems, left for down

        if (stemUp)
        {
            symbol.StemY1 = noteheadY;
            symbol.StemY2 = noteheadY - stemLength;
        }
        else
        {
            symbol.StemY1 = noteheadY;
            symbol.StemY2 = noteheadY + stemLength;
        }
    }

    private static void CalculateBeamedGroup(List<LayoutSymbol> group, double staffBaseline, SvgContext context)
    {
        if (group.Count == 0) return;

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

            stemUp = avgY < staffBaseline;
        }

        // Assign beam group IDs
        var beamGroupId = group[0].GetHashCode();
        
        for (int i = 0; i < group.Count; i++)
        {
            var symbol = group[i];
            symbol.BeamGroupId = beamGroupId;
            symbol.IsFirstInBeamGroup = (i == 0);
            symbol.IsLastInBeamGroup = (i == group.Count - 1);
            symbol.BeamCount = GetBeamCount(symbol);
        }

        // Calculate stems for all notes in the group
        foreach (var symbol in group)
        {
            symbol.StemUp = stemUp;
            var stemLength = StemLength * context.StaffSpace;
            symbol.StemX = symbol.X;

            var noteheadY = symbol.Y;
            if (symbol is ChordLayoutSymbol chordSymbol && chordSymbol.NoteheadYPositions.Count > 0)
            {
                noteheadY = stemUp ? chordSymbol.NoteheadYPositions.Max() : chordSymbol.NoteheadYPositions.Min();
            }

            if (stemUp)
            {
                symbol.StemY1 = noteheadY;
                symbol.StemY2 = noteheadY - stemLength;
            }
            else
            {
                symbol.StemY1 = noteheadY;
                symbol.StemY2 = noteheadY + stemLength;
            }
        }

        // Adjust stem endpoints to meet a horizontal beam
        // The beam position is at the average of all stem ends, adjusted for minimum stem length
        var stemEndYs = group.Select(s => s.StemY2).ToList();
        var beamY = stemUp 
            ? stemEndYs.Min()  // For stems up, beam is at the highest (most negative Y) point
            : stemEndYs.Max(); // For stems down, beam is at the lowest point

        foreach (var symbol in group)
        {
            symbol.StemY2 = beamY;
        }
    }

    private static int GetBeamCount(LayoutSymbol symbol)
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