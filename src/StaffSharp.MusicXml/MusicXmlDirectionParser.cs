namespace StaffSharp.MusicXml;

using StaffSharp.Notation;
using System.Xml.Linq;

/// <summary>
/// Parses MusicXML direction elements (tempo, dynamics, text).
/// </summary>
internal static class MusicXmlDirectionParser
{
    /// <summary>
    /// Parses a direction element and returns Direction objects.
    /// </summary>
    public static List<Direction> ParseDirection(XElement directionElement)
    {
        ArgumentNullException.ThrowIfNull(directionElement);

        var directions = new List<Direction>();

        // Get placement (above/below)
        var placementAttr = directionElement.Attribute("placement");
        var placement = placementAttr?.Value == "below" ? Placement.Below : Placement.Above;

        // Process direction-type elements
        foreach (var directionType in directionElement.Elements("direction-type"))
        {
            // Check for words (text directions like "Allegro", "Fine", etc.)
            var wordsElement = directionType.Element("words");
            if (wordsElement != null)
            {
                var text = wordsElement.Value;
                // Try to determine if it's a tempo marking
                if (IsTempuMarking(text))
                {
                    directions.Add(new Direction(DirectionType.Tempo, placement, text));
                }
                else
                {
                    directions.Add(new Direction(DirectionType.Text, placement, text));
                }
                continue;
            }

            // Check for dynamics
            var dynamicsElement = directionType.Element("dynamics");
            if (dynamicsElement != null)
            {
                var dynamic = ParseDynamic(dynamicsElement);
                if (dynamic != null)
                {
                    directions.Add(new Direction(DirectionType.Dynamic, placement, dynamic));
                }
                continue;
            }

            // Check for metronome marking
            var metronomeElement = directionType.Element("metronome");
            if (metronomeElement != null)
            {
                var (text, bpm) = ParseMetronome(metronomeElement);
                directions.Add(new Direction(DirectionType.Tempo, placement, text, bpm));
                continue;
            }

            // Check for rehearsal marks
            var rehearsalElement = directionType.Element("rehearsal");
            if (rehearsalElement != null)
            {
                directions.Add(new Direction(DirectionType.RehearsalMark, placement, rehearsalElement.Value));
                continue;
            }

            // Check for wedge (crescendo/diminuendo)
            var wedgeElement = directionType.Element("wedge");
            if (wedgeElement != null)
            {
                var wedgeType = wedgeElement.Attribute("type")?.Value;
                if (wedgeType == "crescendo")
                {
                    directions.Add(new Direction(DirectionType.Crescendo, placement, "cresc."));
                }
                else if (wedgeType == "diminuendo")
                {
                    directions.Add(new Direction(DirectionType.Diminuendo, placement, "dim."));
                }
                continue;
            }
        }

        // Check for sound element (for tempo changes)
        var soundElement = directionElement.Element("sound");
        if (soundElement != null)
        {
            var tempoAttr = soundElement.Attribute("tempo");
            if (tempoAttr != null && int.TryParse(tempoAttr.Value, out var tempo))
            {
                // Only add if we don't already have a tempo direction from metronome
                if (!directions.Any(d => d.Type == DirectionType.Tempo && d.Bpm.HasValue))
                {
                    directions.Add(new Direction(DirectionType.Tempo, placement, $"♩ = {tempo}", tempo));
                }
            }
        }

        return directions;
    }

    private static string? ParseDynamic(XElement dynamicsElement)
    {
        // MusicXML has individual elements for each dynamic: <p/>, <mf/>, <ff/>, etc.
        foreach (var child in dynamicsElement.Elements())
        {
            var dynamicName = child.Name.LocalName;
            // Return the dynamic marking
            return dynamicName;
        }
        return null;
    }

    private static (string Text, int Bpm) ParseMetronome(XElement metronomeElement)
    {
        // Parse <beat-unit>quarter</beat-unit> <per-minute>120</per-minute>
        var beatUnit = metronomeElement.Element("beat-unit")?.Value ?? "quarter";
        var perMinuteElement = metronomeElement.Element("per-minute");

        int bpm = 120; // Default
        if (perMinuteElement != null && int.TryParse(perMinuteElement.Value, out var parsedBpm))
        {
            bpm = parsedBpm;
        }

        // Convert beat-unit to note symbol
        var noteSymbol = beatUnit switch
        {
            "whole" => "𝅝",
            "half" => "𝅗𝅥",
            "quarter" => "♩",
            "eighth" => "♪",
            "16th" => "𝅘𝅥𝅯",
            _ => "♩"
        };

        return ($"{noteSymbol} = {bpm}", bpm);
    }

    private static bool IsTempuMarking(string text)
    {
        // Common tempo markings
        var tempoMarkings = new[]
        {
            "Grave", "Largo", "Lento", "Adagio", "Andante", "Moderato",
            "Allegretto", "Allegro", "Vivace", "Presto", "Prestissimo",
            "Larghetto", "Andantino", "Sostenuto", "Maestoso"
        };

        return tempoMarkings.Any(t => text.Contains(t, StringComparison.OrdinalIgnoreCase));
    }
}
