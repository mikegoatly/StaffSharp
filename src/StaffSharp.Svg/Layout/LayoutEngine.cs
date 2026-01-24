namespace StaffSharp.Layout;

using StaffSharp;

using StaffSharp.Layout.Model;
using StaffSharp.Layout.Passes;
using StaffSharp.Notation;

/// <summary>
/// Engine for laying out musical elements.
/// </summary>
internal static class LayoutEngine
{
    internal static ILayoutPass[] LayoutPasses { get; } =
    [
        new VerticalPositionPass(),                 // Y positions relative to staff
        new AccidentalPlacementPass(),              // Which accidentals to show
        new NoteHeadPass(),                         // Notehead position and chord notehead shifts
        new MeasureWidthCalculationPass(),          // Calculate measure widths (for breaking decisions)
        new UnifiedMeasureWidthPass(),              // Unify measure widths across staves for aligned barlines
        new SystemBreakingPass(),                   // Breaks systems and inserts system symbols (clefs, keys, etc.)
        new HorizontalPositionPass(),               // Assigns final X positions
        new DotPositioningPass(),                   // Position augmentation dots (needs X positions)
        new StemAndBeamPass(),                      // Stems and beams (needs X positions for slanted beams)
        new ArticulationPlacementPass(),            // Position articulations/decorations (needs stem direction)
        new SlurAndTiePass(),                       // Creates tie and slur curves from part-level spans (needs final positions)
        new LayoutElementBoundsCalculationPass(),   // Calculates staff bounds (needed before system positioning)
        new SystemGenerationPass(),                 // Positions systems vertically using actual staff heights
        new LayoutBoundsCalculationPass()           // Calculates final system bounds
    ];

    public static LayoutModel Layout(NotationScore score, SvgContext context)
    {
        ArgumentNullException.ThrowIfNull(score);
        ArgumentNullException.ThrowIfNull(context);

        var model = new LayoutModel
        {
            Metadata = score.Metadata,
            Parts = score.Parts
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
        // Store the staff temporarily - SystemGenerationPass will organize into systems
        // For now, add to a single system (will be refactored by SystemGenerationPass)
        var layoutStaffs = score.Parts.SelectMany((p, partIndex) => p.Staves.Select(staff => ConvertStaff(staff, partIndex, score.Metadata, context)))
            .ToList();

        model.Systems.Add(new LayoutSystem(layoutStaffs));
    }

    private static LayoutStaff ConvertStaff(Staff staff, int partIndex, ScoreMetadata metadata, SvgContext context)
    {
        var layoutStaff = new LayoutStaff
        {
            CurrentClef = staff.Clef,
            CurrentKeySignature = metadata.KeySignature,
            PartIndex = partIndex,
            StaffNumber = staff.Number
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

            // Get the measure number (1-based) and time signature
            var firstMeasure = staff.Voices[0].Measures[measureIndex];
            var measureNumber = firstMeasure.Number;

            // Set time signature (use measure-specific or fall back to score default)
            layoutMeasure.TimeSignature = firstMeasure.TimeSignature ?? metadata.TimeSignature;

            // Note: Slurs are now handled at part level via SlurSpanPass, not measure level

            // Add clef at the start of the first measure (before time 0)
            if (measureNumber == 1)
            {
                layoutMeasure.Symbols.Add(ClefLayoutSymbol.Create(staff.Clef, context));
            }

            // Add key signature at the start of the first measure (after clef)
            if (measureNumber == 1 && metadata.KeySignature != KeySignature.C)
            {
                layoutMeasure.Symbols.Add(KeySignatureLayoutSymbol.Create(metadata.KeySignature, staff.Clef, context));
            }

            // Add time signature at the start of the first measure (after key signature)
            if (measureNumber == 1)
            {
                layoutMeasure.Symbols.Add(TimeSignatureLayoutSymbol.Create(metadata.TimeSignature, context));
            }

            // Collect all events from all voices with their time positions
            var allEvents = new List<(INotationEvent Event, double TimePosition, int VoiceNumber)>();

            // Technically each voice could have a different number of measures (one ending early, etc.)
            // So we make sure that we only process voices for which there are enough measures.
            foreach (var voice in staff.Voices.Where(v => measureIndex < v.Measures.Count))
            {
                var measure = voice.Measures[measureIndex];
                double timePosition = 0.0;

                foreach (var notationEvent in measure.Events)
                {
                    allEvents.Add((notationEvent, timePosition, voice.Number));
                    timePosition += GetDurationValue(notationEvent);
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
                layoutMeasure.Symbols.Add(symbol);
            }

            // Find the last time position for the barline
            var lastTimePosition = allEvents.Count > 0
                ? allEvents.Max(e => e.TimePosition + GetDurationValue(e.Event))
                : 0.0;

            // Add start barline if specified
            if (firstMeasure.StartBarline.HasValue)
            {
                var startBarlineSymbol = new BarlineLayoutSymbol
                {
                    BarlineType = firstMeasure.StartBarline.Value,
                    TimePosition = -0.5
                };
                layoutMeasure.Symbols.Add(startBarlineSymbol);
            }

            // Add barline at the end of the measure
            var endBarlineType = firstMeasure.EndBarline ?? BarlineType.Normal;
            var barlineSymbol = new BarlineLayoutSymbol
            {
                BarlineType = endBarlineType,
                TimePosition = lastTimePosition
            };
            layoutMeasure.Symbols.Add(barlineSymbol);

            layoutStaff.Measures.Add(layoutMeasure);
        }

        return layoutStaff;
    }

    private static LayoutSymbol ConvertEventToSymbol(INotationEvent notationEvent, double timePosition)
    {
        return notationEvent switch
        {
            NotationNote note => new NoteLayoutSymbol
            {
                Note = note,
                TimePosition = timePosition,
                DotCount = note.Duration.Dots
            },
            Rest rest => new RestLayoutSymbol
            {
                Rest = rest,
                TimePosition = timePosition,
                DotCount = rest.Duration.Dots
            },
            Chord chord => new ChordLayoutSymbol
            {
                Chord = chord,
                TimePosition = timePosition,
                DotCount = chord.Duration.Dots
            },
            _ => throw new NotSupportedException($"Event type {notationEvent.GetType().Name} is not supported")
        };
    }

    private static double GetDurationValue(INotationEvent notationEvent)
    {
        // Convert duration to beats (quarter note = 1.0)
        var rational = notationEvent.Duration.ToBeats();
        return (double)rational.Numerator / rational.Denominator;
    }
}
