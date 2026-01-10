namespace StaffSharp.Layout.Passes;

using StaffSharp;
using StaffSharp.Layout.Model;
using StaffSharp.Notation;

using StaffSymbolMap = Dictionary<(int PartIndex, int StaffNumber), Dictionary<Notation.INotationEvent, CurveSymbolInfo>>;

/// <summary>
/// Creates curve elements for both ties and slurs from part-level TieSpan/SlurSpan objects.
/// Supports cross-system curves with proper tapering.
/// </summary>
internal sealed class SlurAndTiePass : ILayoutPass
{
    public void Run(LayoutModel model, SvgContext context)
    {
        // Check if there are any ties or slurs to process
        if (!model.Parts.SelectMany(p => p.Ties).Any()
            && !model.Parts.SelectMany(p => p.Slurs).Any())
        {
            return; // Nothing to process
        }

        foreach (var system in model.Systems)
        {
            // Build enhanced symbol maps per staff in this system
            var staffSymbolMaps = BuildSymbolMaps(system);

            // Process ties and slurs for each part
            for (int partIndex = 0; partIndex < model.Parts.Count; partIndex++)
            {
                var part = model.Parts[partIndex];

                // Process ties
                foreach (var span in part.Ties)
                {
                    ProcessSpan(span, partIndex, system, model, staffSymbolMaps, context, isTie: true);
                }

                // Process slurs
                foreach (var span in part.Slurs)
                {
                    ProcessSpan(span, partIndex, system, model, staffSymbolMaps, context, isTie: false);
                }
            }
        }
    }

    /// <summary>
    /// Builds a map of notation events to their layout symbols for the current system.
    /// </summary>
    private static StaffSymbolMap BuildSymbolMaps(
        LayoutSystem system)
    {
        var staffSymbolMaps = new StaffSymbolMap();

        foreach (var staff in system.Staves)
        {
            var key = (staff.PartIndex, staff.StaffNumber);
            var map = new Dictionary<INotationEvent, CurveSymbolInfo>(ReferenceEqualityComparer<INotationEvent>.Instance);

            foreach (var measure in staff.Measures)
            {
                foreach (var symbol in measure.Symbols)
                {
                    switch (symbol)
                    {
                        case NoteLayoutSymbol n:
                            map[n.Note] = new CurveSymbolInfo(system, measure, n);
                            break;
                        case ChordLayoutSymbol c:
                            map[c.Chord] = new CurveSymbolInfo(system, measure, c);
                            break;
                    }
                }
            }

            staffSymbolMaps[key] = map;
        }

        return staffSymbolMaps;
    }

    /// <summary>
    /// Processes a single tie or slur span, creating the appropriate curve segment(s).
    /// </summary>
    private static void ProcessSpan(
        TieSpan span,
        int partIndex,
        LayoutSystem currentSystem,
        LayoutModel model,
        StaffSymbolMap staffSymbolMaps,
        SvgContext context,
        bool isTie)
    {
        var startKey = (partIndex, span.StartStaffNumber);
        var endKey = (partIndex, span.EndStaffNumber);

        var startSymbolInfo = staffSymbolMaps.GetValueOrDefault(startKey)?.GetValueOrDefault(span.StartEvent);
        var endSymbolInfo = staffSymbolMaps.GetValueOrDefault(endKey)?.GetValueOrDefault(span.EndEvent);

        LayoutCurve curve;

        if (startSymbolInfo is { Symbol: { } startSymbol, System: { } startSystem, Measure: { } startMeasure })
        {
            if (endSymbolInfo is { Symbol: { } endSymbol })
            {
                // Case 1: Both endpoints in this system
                var startHalfWidth = context.GetNoteheadWidth(startSymbol.Duration.Base) / 2.0;
                var endHalfWidth = context.GetNoteheadWidth(endSymbol.Duration.Base) / 2.0;
                curve = LayoutCurve.Create(
                    startSymbol,
                    startSymbol.X + startHalfWidth,
                    startSymbol.Y,
                    endSymbol.X + endHalfWidth,
                    endSymbol.Y,
                    context,
                    CurveEndTaper.Both,
                    isTie);

                startMeasure.Curves.Add(curve);
            }
            else
            {
                // Case 2: Starts here, continues to next system
                var endX = startSystem.X + startSystem.Width - (context.StaffSpace * 0.5);
                var endY = startSymbol.Stem.Up
                    ? startSymbol.Y + (context.StaffSpace * 2.0)
                    : startSymbol.Y - (context.StaffSpace * 2.0);

                var startHalfWidth = context.GetNoteheadWidth(startSymbol.Duration.Base) / 2.0;
                curve = LayoutCurve.Create(
                    startSymbol,
                    startSymbol.X + startHalfWidth,
                    startSymbol.Y,
                    endX,
                    endY,
                    context,
                    CurveEndTaper.Start,
                    isTie);

                startMeasure.Curves.Add(curve);
            }
        }
        else if (endSymbolInfo is { Symbol: { } endSymbol, System: { } endSystem, Measure: { } endMeasure })
        {
            // Case 3: Ends here, started in previous system
            var startX = endSystem.X + (context.StaffSpace * 0.5);
            var startY = endSymbol.Stem.Up
                ? endSymbol.Y + (context.StaffSpace * 2.0)
                : endSymbol.Y - (context.StaffSpace * 2.0);

            var endHalfWidth = context.GetNoteheadWidth(endSymbol.Duration.Base) / 2.0;
            curve = LayoutCurve.Create(
                endSymbol,
                startX,
                startY,
                endSymbol.X + endHalfWidth,
                endSymbol.Y,
                context,
                CurveEndTaper.End,
                isTie);

            endMeasure.Curves.Add(curve);
        }
        else if (IsMiddleOfSpan(span, currentSystem, model, partIndex))
        {
            // Case 4: Neither endpoint in this system - middle segment
            curve = LayoutCurve.CreateCrossSystem(currentSystem, context, isTie);

            // Add to first measure of first staff in the system
            if (currentSystem.Staves is [{ Measures: [{ } measure, ..] }, ..])
            {
                measure.Curves.Add(curve);
            }
        }
    }

    /// <summary>
    /// Determines if the current system is in the middle of a span
    /// (span starts before this system and ends after this system).
    /// </summary>
    private static bool IsMiddleOfSpan(TieSpan span, LayoutSystem currentSystem, LayoutModel model, int partIndex)
    {
        int? startSystemIndex = null;
        int? endSystemIndex = null;

        for (int i = 0; i < model.Systems.Count; i++)
        {
            var system = model.Systems[i];
            foreach (var staff in system.Staves.Where(s => s.PartIndex == partIndex))
            {
                // Check if this staff contains the start event
                if (staff.StaffNumber == span.StartStaffNumber && staff.Contains(span.StartEvent))
                {
                    startSystemIndex = i;
                }

                // Check if this staff contains the end event
                if (staff.StaffNumber == span.EndStaffNumber && staff.Contains(span.EndEvent))
                {
                    endSystemIndex = i;
                }

                if (startSystemIndex.HasValue && endSystemIndex.HasValue)
                {
                    break;
                }
            }
            if (startSystemIndex.HasValue && endSystemIndex.HasValue)
            {
                break;
            }
        }

        // Current system index
        var currentSystemIndex = model.Systems.IndexOf(currentSystem);

        // Middle segment if start is before and end is after current system
        return startSystemIndex.HasValue && endSystemIndex.HasValue &&
               startSystemIndex.Value < currentSystemIndex &&
               endSystemIndex.Value > currentSystemIndex;
    }
}

/// <summary>
/// Bundles symbol location information for cross-system tracking.
/// </summary>
internal record struct CurveSymbolInfo(LayoutSystem System, LayoutMeasure Measure, IStemmedSymbol Symbol);
