namespace StaffSharp;

using System.Xml.Linq;

using StaffSharp.Layout;
using StaffSharp.Layout.Model;
using StaffSharp.Notation;

/// <summary>
/// Represents a rendered musical score as SVG XML that supports dynamic highlighting.
/// </summary>
public sealed class RenderedScore
{
    private readonly XElement _svgRoot;
    private readonly LayoutModel _layoutModel;
    private readonly NotationScore _score;
    private readonly string _foregroundColor;
    private HashSet<string> _currentHighlightedIds = [];
    private ILookup<string, XElement>? _elementLookup;

    internal RenderedScore(XElement svgRoot, LayoutModel layoutModel, NotationScore score, string foregroundColor)
    {
        _svgRoot = svgRoot;
        _layoutModel = layoutModel;
        _score = score;
        _foregroundColor = foregroundColor;
    }

    public XElement SvgRoot => _svgRoot;

    /// <summary>
    /// Updates the highlight state of the rendered score based on a time range.
    /// </summary>
    /// <param name="startTime">The start time for highlighting. If null, all highlights are cleared.</param>
    /// <param name="endTime">The end time for highlighting. If null, only notes at startTime are highlighted.</param>
    /// <param name="highlightColor">The color to use for highlighted elements (e.g., "#FF0000").</param>
    /// <returns><c>true</c> if highlighting effected changes in the SVG, otherwise <c>false</c>.</returns>
    public bool Highlight(TimeSpan? startTime, TimeSpan? endTime, string highlightColor)
    {
        // Calculate which symbols should be highlighted
        HashSet<string> highlightedIds = [];

        if (startTime.HasValue && _score.TempoMap != null)
        {
            highlightedIds = CalculateHighlightedSymbols(
                startTime.Value.TotalSeconds,
                endTime?.TotalSeconds);
        }

        if (highlightedIds.SetEquals(_currentHighlightedIds))
        {
            // No change in highlighting
            return false;
        }

        var elementLookup = GetOrBuildElementLookup();

        // Any elements to un-highlight
        var toUnhighlight = _currentHighlightedIds
            .Except(highlightedIds);

        var toHighlight = highlightedIds
            .Except(_currentHighlightedIds);

        UpdateColors(toUnhighlight.Select(x => elementLookup[x]).SelectMany(e => e), _foregroundColor);
        UpdateColors(toHighlight.Select(x => elementLookup[x]).SelectMany(e => e), highlightColor);

        _currentHighlightedIds = highlightedIds;
        return true;
    }

    private ILookup<string, XElement> GetOrBuildElementLookup()
    {
        if (_elementLookup is null)
        {
            _elementLookup = EnumerateElementsWithIds(_svgRoot)
                .ToLookup(t => t.id, t => t.element);
        }

        return _elementLookup;
    }

    private static IEnumerable<(string id, XElement element)> EnumerateElementsWithIds(XElement node)
    {
        if (node.Attribute("data-symbol-id")?.Value is { } symbolId)
        {
            yield return (symbolId, node);
        }

        foreach (var child in node.Elements())
        {
            foreach (var descendant in EnumerateElementsWithIds(child))
            {
                yield return descendant;
            }
        }
    }

    private static void UpdateColors(IEnumerable<XElement> elements, string color)
    {
        foreach (var element in elements)
        {
            if (element.Attribute("fill") is { } fillAttr)
            {
                fillAttr.Value = color;
            }

            if (element.Attribute("stroke") is { } strokeAttr)
            {
                strokeAttr.Value = color;
            }
        }
    }

    /// <summary>
    /// Calculates which symbols should be highlighted based on a time range.
    /// Optimized to skip measures outside the highlight range.
    /// </summary>
    /// <param name="layoutModel">The layout model containing all symbols.</param>
    /// <param name="tempoMap">The tempo map for time-to-beat conversion.</param>
    /// <param name="highlightStartSeconds">Start time in seconds (null to disable highlighting).</param>
    /// <param name="highlightEndSeconds">End time in seconds (null to highlight only the start moment).</param>
    /// <returns>Set of symbol IDs that should be highlighted.</returns>
    private HashSet<string> CalculateHighlightedSymbols(
        double highlightStartSeconds,
        double? highlightEndSeconds)
    {
        if (_score.TempoMap is null)
        {
            return [];
        }

        var startBeat = _score.TempoMap.GetBeatAtTime(highlightStartSeconds);
        var endBeat = highlightEndSeconds.HasValue
            ? _score.TempoMap.GetBeatAtTime(highlightEndSeconds.GetValueOrDefault())
            : startBeat;

        var result = new HashSet<string>();

        // Measures are in chronological order - process until we pass the highlight range
        foreach (var measure in _layoutModel.Systems
            .SelectMany(s => s.Staves)
            .SelectMany(s => s.Measures))
        {
            // Calculate measure end beat for early exit optimization
            var measureEndBeat = measure.StartBeat;
            if (measure.TimeSignature != null)
            {
                var duration = measure.TimeSignature.BeatsPerMeasure;
                measureEndBeat += (double)duration.Numerator / duration.Denominator;
            }

            // Skip measures that end before highlight starts
            if (measureEndBeat < startBeat)
            {
                continue;
            }

            // Early exit: if measure starts after highlight ends, all subsequent measures are also past it
            if (measure.StartBeat > endBeat)
            {
                break;
            }

            // Measure overlaps with highlight range - check its symbols
            foreach (var symbol in measure.Symbols.OfType<AugmentationDottedLayoutSymbol>())
            {
                if (ShouldHighlight(symbol, measure, startBeat, endBeat))
                {
                    result.Add(symbol.Id);
                }
            }
        }

        return result;
    }

    private static bool ShouldHighlight(AugmentationDottedLayoutSymbol symbol, LayoutMeasure measure, double startBeat, double endBeat)
    {
        // Calculate absolute beat position: measure start + symbol's relative position
        var symbolStartBeat = measure.StartBeat + symbol.TimePosition;
        var symbolDuration = GetSymbolDuration(symbol);
        var symbolEndBeat = symbolStartBeat + symbolDuration;

        // Check for overlap: symbol overlaps with range if:
        // symbolStart <= rangeEnd AND symbolEnd > rangeStart
        return symbolStartBeat <= endBeat && symbolEndBeat > startBeat;
    }

    private static double GetSymbolDuration(LayoutSymbol symbol)
    {
        SymbolicDuration? duration = symbol switch
        {
            NoteLayoutSymbol note => note.Note.Duration,
            ChordLayoutSymbol chord => chord.Chord.Duration,
            RestLayoutSymbol rest => rest.Rest.Duration,
            _ => null
        };

        if (duration == null)
        {
            return 0.0;
        }

        var beats = duration.Value.ToBeats();
        return (double)beats.Numerator / beats.Denominator;
    }

    /// <summary>
    /// Returns the SVG representation as a string.
    /// </summary>
    public override string ToString()
    {
        return _svgRoot.ToString();
    }
}
