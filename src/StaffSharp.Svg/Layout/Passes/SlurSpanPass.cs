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

        for (int systemIndex = 0; systemIndex < model.Systems.Count; systemIndex++)
        {
            var system = model.Systems[systemIndex];

            // Build event->symbol maps per staff in this system
            var staffSymbolMaps = new Dictionary<(int PartIndex, int StaffNumber), Dictionary<INotationEvent, IStemmedSymbol>>();

            foreach (var staff in system.Staves)
            {
                var key = (staff.PartIndex, staff.StaffNumber);

                // Force reference comparison to match exact event instances
                // (INotationEvent implementations may use value equality, but we need identity)
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

            // For each part, emit slur segments considering system boundaries
            for (int partIndex = 0; partIndex < model.Parts.Count; partIndex++)
            {
                var part = model.Parts[partIndex];
                foreach (var span in part.Slurs)
                {
                    var startKey = (partIndex, span.StartStaffNumber);
                    var endKey = (partIndex, span.EndStaffNumber);

                    staffSymbolMaps.TryGetValue(startKey, out var startMap);
                    staffSymbolMaps.TryGetValue(endKey, out var endMap);

                    // Determine which systems contain the actual endpoints
                    var (startSystemIdx, endSystemIdx) = FindEndpointSystems(model, partIndex, span);

                    if (startSystemIdx < 0 || endSystemIdx < 0 || systemIndex < startSystemIdx || systemIndex > endSystemIdx)
                    {
                        continue; // this system is outside the span
                    }

                    IStemmedSymbol? startSymHere = null;
                    IStemmedSymbol? endSymHere = null;
                    var hasStartHere = startMap != null && startMap.TryGetValue(span.StartEvent, out startSymHere);
                    var hasEndHere = endMap != null && endMap.TryGetValue(span.EndEvent, out endSymHere);

                    IStemmedSymbol? segStart = null;
                    IStemmedSymbol? segEnd = null;
                    bool contStart = false, contEnd = false;

                    if (hasStartHere && hasEndHere)
                    {
                        segStart = startSymHere!;
                        segEnd = endSymHere!;
                    }
                    else if (systemIndex == startSystemIdx && !hasEndHere)
                    {
                        // start here, continues to next system
                        segStart = startSymHere!;
                        segEnd = FindLastSymbolInSystem(system, endKey, span.EndVoiceNumber);
                        contEnd = true;
                    }
                    else if (systemIndex == endSystemIdx && !hasStartHere)
                    {
                        // ends here, started in previous system
                        segStart = FindFirstSymbolInSystem(system, startKey, span.StartVoiceNumber);
                        segEnd = endSymHere!;
                        contStart = true;
                    }
                    else
                    {
                        // middle segment between start and end systems
                        segStart = FindFirstSymbolInSystem(system, startKey, span.StartVoiceNumber);
                        segEnd = FindLastSymbolInSystem(system, endKey, span.EndVoiceNumber);
                        contStart = true;
                        contEnd = true;
                    }

                    if (segStart == null || segEnd == null)
                    {
                        continue; // cannot construct a meaningful segment in this system
                    }

                    // Reuse existing tie/slur curve math
                    var curve = LayoutCurve.Create(segStart, segEnd, context, isTie: false);
                    curve.ContinuationStart = contStart;
                    curve.ContinuationEnd = contEnd;

                    // Add the curve to the measure where the segment end symbol lives
                    var endMeasure = FindMeasureContainingSymbol(system, endKey, segEnd);

                    endMeasure.AddCurve(curve);
                }
            }
        }
    }

    private static (int startSystemIdx, int endSystemIdx) FindEndpointSystems(LayoutModel model, int partIndex, SlurSpan span)
    {
        int startIdx = -1, endIdx = -1;
        for (int i = 0; i < model.Systems.Count; i++)
        {
            var sys = model.Systems[i];
            foreach (var staff in sys.Staves.Where(s => s.PartIndex == partIndex))
            {
                if (staff.StaffNumber == span.StartStaffNumber && ContainsEvent(staff, span.StartEvent))
                {
                    startIdx = i;
                }

                if (staff.StaffNumber == span.EndStaffNumber && ContainsEvent(staff, span.EndEvent))
                {
                    endIdx = i;
                }
            }
        }

        return (startIdx, endIdx);
    }

    private static bool ContainsEvent(LayoutStaff staff, INotationEvent ev)
    {
        return staff.Measures
            .SelectMany(m => m.Symbols)
            .Any(s => 
                (s is NoteLayoutSymbol n && ReferenceEquals(n.Note, ev)) 
                || (s is ChordLayoutSymbol c && ReferenceEquals(c.Chord, ev)));

    }

    private static IStemmedSymbol? FindFirstSymbolInSystem(LayoutSystem system, (int PartIndex, int StaffNumber) key, int voiceNumber)
    {
        var staff = system.Staves.FirstOrDefault(s => s.PartIndex == key.PartIndex && s.StaffNumber == key.StaffNumber);

        return staff?.Measures
            .SelectMany(m => m.Symbols.OfType<IStemmedSymbol>())
            .FirstOrDefault(s => s.VoiceNumber == voiceNumber);
    }

    private static IStemmedSymbol? FindLastSymbolInSystem(LayoutSystem system, (int PartIndex, int StaffNumber) key, int voiceNumber)
    {
        var staff = system.Staves.FirstOrDefault(s => s.PartIndex == key.PartIndex && s.StaffNumber == key.StaffNumber);
        if (staff == null) return null;
        for (int mi = staff.Measures.Count - 1; mi >= 0; mi--)
        {
            var measure = staff.Measures[mi];
            for (int si = measure.Symbols.Count - 1; si >= 0; si--)
            {
                var symbol = measure.Symbols[si];
                if (symbol is IStemmedSymbol stemmed && symbol.VoiceNumber == voiceNumber)
                {
                    return stemmed;
                }
            }
        }
        return null;
    }

    private static LayoutMeasure FindMeasureContainingSymbol(LayoutSystem system, (int PartIndex, int StaffNumber) endKey, IStemmedSymbol segEnd)
    {
        var staff = system.Staves.First(s => s.PartIndex == endKey.PartIndex && s.StaffNumber == endKey.StaffNumber);
        for (int i = 0; i < staff.Measures.Count; i++)
        {
            var m = staff.Measures[i];
            if (m.Symbols.Contains((LayoutSymbol)segEnd))
            {
                return m;
            }
        }

        // Fallback to last measure if not found (should not happen)
        return staff.Measures[staff.Measures.Count - 1];
    }
}