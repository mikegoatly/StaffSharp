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

    private static bool DetermineStemDirection(double noteheadY, double staffBaseline, int voiceNumber, bool? forcedDirection)
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

            // In SVG coordinates, Y increases downward
            // Notes below middle line (higher Y) → stems up
            // Notes above middle line (lower Y) → stems down
            stemUp = avgY > staffBaseline;
        }

        // Assign beam group IDs and calculate stem attachment points
        var beamGroupId = group[0].GetHashCode();

        for (int i = 0; i < group.Count; i++)
        {
            var symbol = group[i];
            symbol.BeamGroupId = beamGroupId;
            symbol.IsFirstInBeamGroup = (i == 0);
            symbol.IsLastInBeamGroup = (i == group.Count - 1);
            symbol.BeamCount = GetBeamCount(symbol);
            symbol.StemUp = stemUp;

            // Stem X position: fixed offset from notehead
            // These match the rendering offsets: right edge for stem up, left edge for stem down
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

        // Set StemY2 for each note to meet the slanted beam at its stem X position
        foreach (var symbol in group)
        {
            var beamYAtThisNote = beamY1 + (symbol.StemX - firstSymbol.StemX) * beamSlope;
            symbol.StemY2 = beamYAtThisNote;
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