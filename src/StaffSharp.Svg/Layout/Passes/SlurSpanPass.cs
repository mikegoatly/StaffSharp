namespace StaffSharp.Layout.Passes;

using StaffSharp;
using StaffSharp.Layout.Model;
using StaffSharp.Notation;

/// <summary>
/// Creates slur curves from part-level SlurSpans. First version only emits segments
/// when both endpoints of a span are present in the same system.
/// </summary>
internal sealed class SlurSpanPass : ILayoutPass
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

            // For each part, emit slurs that have both endpoints in this system
            for (int partIndex = 0; partIndex < model.Parts.Count; partIndex++)
            {
                var part = model.Parts[partIndex];
                foreach (var span in part.Slurs)
                {
                    var startKey = (partIndex, span.StartStaffNumber);
                    var endKey = (partIndex, span.EndStaffNumber);

                    if (!staffSymbolMaps.TryGetValue(startKey, out var startMap) ||
                        !staffSymbolMaps.TryGetValue(endKey, out var endMap))
                    {
                        continue; // endpoints not in this system
                    }

                    if (!startMap.TryGetValue(span.StartEvent, out var startSym) ||
                        !endMap.TryGetValue(span.EndEvent, out var endSym))
                    {
                        continue; // at least one endpoint not present in this system
                    }

                    // Reuse existing tie/slur curve math
                    var curve = LayoutCurve.Create(startSym, endSym, context, isTie: false);

                    // Add the curve to the measure where the end symbol lives (simple choice)
                    // Find that measure
                    var endMeasure = system.Staves
                        .First(s => s.PartIndex == endKey.partIndex && s.StaffNumber == endKey.EndStaffNumber)
                        .Measures
                        .First(m => m.Symbols.Contains((LayoutSymbol)endSym));

                    endMeasure.AddCurve(curve);
                }
            }
        }
    }
}
