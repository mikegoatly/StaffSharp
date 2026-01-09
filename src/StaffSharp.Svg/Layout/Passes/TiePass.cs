namespace StaffSharp.Layout.Passes;

using StaffSharp;
using StaffSharp.Layout.Model;
using StaffSharp.Notation;

/// <summary>
/// Creates tie curves from part-level TieSpans. Only emits segments
/// when both endpoints of a span are present in the same system.
/// </summary>
internal sealed class TiePass : ILayoutPass
{
    public void Run(LayoutModel model, SvgContext context)
    {
        if (model.Parts is null || model.Parts.Count == 0)
        {
            return;
        }

        foreach (var system in model.Systems)
        {
            // Build event->symbol maps per staff in this system
            var staffSymbolMaps = new Dictionary<(int PartIndex, int StaffNumber), Dictionary<INotationEvent, IStemmedSymbol>>();

            foreach (var staff in system.Staves)
            {
                var key = (staff.PartIndex, staff.StaffNumber);

                // Force reference comparison to match exact event instances
                var map = new Dictionary<INotationEvent, IStemmedSymbol>(ReferenceEqualityComparer<INotationEvent>.Instance);

                foreach (var measure in staff.Measures)
                {
                    foreach (var symbol in measure.Symbols)
                    {
                        switch (symbol)
                        {
                            case NoteLayoutSymbol n:
                                map[n.Note] = n;
                                break;
                            case ChordLayoutSymbol c:
                                map[c.Chord] = c;
                                break;
                        }
                    }
                }

                staffSymbolMaps[key] = map;
            }

            // For each part, emit tie curves
            for (int partIndex = 0; partIndex < model.Parts.Count; partIndex++)
            {
                var part = model.Parts[partIndex];
                foreach (var span in part.Ties)
                {
                    var startKey = (partIndex, span.StartStaffNumber);
                    var endKey = (partIndex, span.EndStaffNumber);

                    staffSymbolMaps.TryGetValue(startKey, out var startMap);
                    staffSymbolMaps.TryGetValue(endKey, out var endMap);

                    IStemmedSymbol? startSymbol = null;
                    IStemmedSymbol? endSymbol = null;

                    var hasStart = startMap != null && startMap.TryGetValue(span.StartEvent, out startSymbol);
                    var hasEnd = endMap != null && endMap.TryGetValue(span.EndEvent, out endSymbol);

                    // Only create tie if both endpoints are in this system
                    if (hasStart && hasEnd && startSymbol != null && endSymbol != null)
                    {
                        var curve = LayoutCurve.Create(startSymbol, endSymbol, context, isTie: true);

                        // Add the curve to the measure where the end symbol lives
                        var endMeasure = FindMeasureContainingSymbol(system, endKey, endSymbol);
                        endMeasure.AddCurve(curve);
                    }
                }
            }
        }
    }

    private static LayoutMeasure FindMeasureContainingSymbol(LayoutSystem system, (int PartIndex, int StaffNumber) key, IStemmedSymbol symbol)
    {
        var staff = system.Staves.First(s => s.PartIndex == key.PartIndex && s.StaffNumber == key.StaffNumber);
        for (int i = 0; i < staff.Measures.Count; i++)
        {
            var m = staff.Measures[i];
            if (m.Symbols.Contains((LayoutSymbol)symbol))
            {
                return m;
            }
        }

        // Fallback to last measure if not found (should not happen)
        return staff.Measures[staff.Measures.Count - 1];
    }
}
