namespace StaffSharp.Svg;

using StaffSharp.Notation;
using StaffSharp.Svg.Layout;
using StaffSharp.Svg.Layout.Passes;

/// <summary>
/// Engine for laying out musical elements.
/// </summary>
public static class LayoutEngine
{
    internal static ILayoutPass[] LayoutPasses { get; } =
    [
        new VerticalPositionPass(),          // Y positions relative to staff
        new AccidentalPlacementPass(),       // Which accidentals to show
        new HeadShiftPass(),                 // Chord notehead shifts
        new MeasureWidthCalculationPass(),   // Calculate measure widths (for breaking decisions)
        new SystemBreakingPass(),            // Breaks systems based on measure widths
        new SystemSymbolInsertionPass(),     // Inserts system symbols (clefs, keys, etc.)
        new HorizontalPositionPass(),        // Assigns final X positions
        new SystemGenerationPass(),          // Generates system layout
        new StemAndBeamPass(),               // Stems and beams (needs X positions for slanted beams)
        new TieAndSlurPass(),                // Creates tie/slur curves (needs final positions)
        new BoundsCalculationPass()          // Calculates accurate bounds (MUST be last)
    ];

    public static LayoutModel Layout(NotationScore score, SvgContext context)
    {
        ArgumentNullException.ThrowIfNull(score);
        ArgumentNullException.ThrowIfNull(context);

        var model = new LayoutModel
        {
            Metadata = score.Metadata
        };

        // Convert notation structure to layout structure
        ConvertScoreToLayout(score, model, context);

        foreach (var pass in LayoutPasses)
        {
            pass.Run(model, context);

            if (context.BailAfterPass is not null && context.BailAfterPass == pass.GetType().Name)
            {
                break;
            }
        }

        return model;
    }

    private static void ConvertScoreToLayout(NotationScore score, LayoutModel model, SvgContext context)
    {
        // Create systems and staves but don't populate measures yet
        // SystemGenerationPass will handle grouping measures into systems
        foreach (var part in score.Parts)
        {
            foreach (var staff in part.Staves)
            {
                var layoutStaff = ConvertStaff(staff, score.Metadata, context);
                
                // Store the staff temporarily - SystemGenerationPass will organize into systems
                // For now, add to a single system (will be refactored by SystemGenerationPass)
                if (model.Systems.Count == 0)
                {
                    model.AddSystem(new LayoutSystem());
                }
                model.Systems[0].AddStaff(layoutStaff);
            }
        }
    }

    private static LayoutStaff ConvertStaff(Staff staff, ScoreMetadata metadata, SvgContext context)
    {
        var layoutStaff = new LayoutStaff
        {
            CurrentClef = staff.Clef,
            CurrentKeySignature = metadata.KeySignature
        };

        if (staff.Voices.Count == 0)
        {
            return layoutStaff;
        }

        // Get measure count from first voice (all voices should have same number of measures)
        var measureCount = staff.Voices[0].Measures.Count;

        for (int measureIndex = 0; measureIndex < measureCount; measureIndex++)
        {
            var layoutMeasure = new LayoutMeasure();
            
            // Get the measure number (1-based)
            var measureNumber = staff.Voices[0].Measures[measureIndex].Number;
            
            // Add clef at the start of the first measure (before time 0)
            if (measureNumber == 1)
            {
                var clefSymbol = new ClefLayoutSymbol { Clef = staff.Clef, TimePosition = -3.0 };
                layoutMeasure.AddSymbol(clefSymbol);
            }

            // Add key signature at the start of the first measure (after clef)
            if (measureNumber == 1 && metadata.KeySignature != KeySignature.C)
            {
                var keySymbol = new KeySignatureLayoutSymbol { KeySignature = metadata.KeySignature, Clef = staff.Clef, TimePosition = -2.0 };
                layoutMeasure.AddSymbol(keySymbol);
            }

            // Add time signature at the start of the first measure (after key signature)
            if (measureNumber == 1)
            {
                var timeSymbol = new TimeSignatureLayoutSymbol { TimeSignature = metadata.TimeSignature, TimePosition = -1.0 };
                layoutMeasure.AddSymbol(timeSymbol);
            }

            // Collect all events from all voices with their time positions
            var allEvents = new List<(INotationEvent Event, double TimePosition, int VoiceNumber)>();
            
            foreach (var voice in staff.Voices)
            {
                if (measureIndex < voice.Measures.Count)
                {
                    var measure = voice.Measures[measureIndex];
                    double timePosition = 0.0;
                    
                    foreach (var notationEvent in measure.Events)
                    {
                        allEvents.Add((notationEvent, timePosition, voice.Number));
                        timePosition += GetDurationValue(notationEvent);
                    }
                }
            }

            // Sort events by time position, then by voice number
            allEvents.Sort((a, b) =>
            {
                var timeCompare = a.TimePosition.CompareTo(b.TimePosition);
                return timeCompare != 0 ? timeCompare : a.VoiceNumber.CompareTo(b.VoiceNumber);
            });

            // Convert events to symbols
            foreach (var (notationEvent, timePosition, voiceNumber) in allEvents)
            {
                var symbol = ConvertEventToSymbol(notationEvent, timePosition);
                symbol.VoiceNumber = voiceNumber;
                layoutMeasure.AddSymbol(symbol);
            }

            // Find the last time position for the barline
            var lastTimePosition = allEvents.Count > 0 
                ? allEvents.Max(e => e.TimePosition + GetDurationValue(e.Event)) 
                : 0.0;

            // Add barline at the end of the measure
            var barlineSymbol = new BarlineLayoutSymbol { BarlineType = BarlineType.Normal, TimePosition = lastTimePosition };
            layoutMeasure.AddSymbol(barlineSymbol);

            layoutStaff.AddMeasure(layoutMeasure);
        }

        return layoutStaff;
    }

    private static LayoutSymbol ConvertEventToSymbol(INotationEvent notationEvent, double timePosition)
    {
        return notationEvent switch
        {
            NotationNote note => new NoteLayoutSymbol { Note = note, TimePosition = timePosition },
            Rest rest => new RestLayoutSymbol { Rest = rest, TimePosition = timePosition },
            Chord chord => new ChordLayoutSymbol { Chord = chord, TimePosition = timePosition },
            _ => throw new NotSupportedException($"Event type {notationEvent.GetType().Name} is not supported")
        };
    }

    private static double GetDurationValue(INotationEvent notationEvent)
    {
        var duration = notationEvent switch
        {
            NotationNote note => note.Duration,
            Rest rest => rest.Duration,
            Chord chord => chord.Duration,
            _ => SymbolicDuration.Quarter
        };

        // Convert duration to beats (quarter note = 1.0)
        var rational = duration.ToBeats();
        return (double)rational.Numerator / rational.Denominator;
    }
}

/// <summary>
/// The root of the layout model.
/// </summary>
public class LayoutModel
{
    public IReadOnlyList<LayoutSystem> Systems => _systems;
    private readonly List<LayoutSystem> _systems = new();

    /// <summary>
    /// Gets the total width of all content, calculated from system bounds.
    /// </summary>
    public double TotalWidth => Systems.Count > 0 
        ? Systems.Max(s => s.X + s.Width) 
        : 0;

    /// <summary>
    /// Gets the total height of all content, calculated from system bounds.
    /// </summary>
    public double TotalHeight => Systems.Count > 0
        ? Systems.Max(s => s.Y + s.Height)
        : 0;

    /// <summary>
    /// Score metadata needed for system symbol insertion (time signature, etc.)
    /// </summary>
    public ScoreMetadata? Metadata { get; set; }

    internal void AddSystem(LayoutSystem system) => _systems.Add(system);

    internal void ClearSystems() => _systems.Clear();

    internal void ReplaceSystems(IEnumerable<LayoutSystem> newSystems)
    {
        _systems.Clear();
        _systems.AddRange(newSystems);
    }
}