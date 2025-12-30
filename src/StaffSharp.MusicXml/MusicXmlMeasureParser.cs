namespace StaffSharp.MusicXml;

using StaffSharp.Notation;
using System.Xml.Linq;

/// <summary>
/// Parses a MusicXML measure element.
/// </summary>
internal static class MusicXmlMeasureParser
{
    /// <summary>
    /// Parses a measure element and returns measures grouped by voice number.
    /// </summary>
    public static Dictionary<int, Measure> ParseMeasure(XElement measureElement, MusicXmlContext context, int measureNumber)
    {
        ArgumentNullException.ThrowIfNull(measureElement);
        ArgumentNullException.ThrowIfNull(context);

        // Group events by voice
        var voiceEvents = new Dictionary<int, List<INotationEvent>>();
        var defaultVoice = 1;
        var directions = new List<Direction>();
        BarlineType? startBarline = null;
        BarlineType? endBarline = null;
        List<int>? repeatVariants = null;

        // Process measure elements in order
        foreach (var element in measureElement.Elements())
        {
            switch (element.Name.LocalName)
            {
                case "attributes":
                    // Update context with new attributes (divisions, key, time, clef)
                    MusicXmlAttributesParser.ParseAttributes(element, context);
                    break;

                case "note":
                    // Parse note and add to appropriate voice
                    var (noteEvent, voiceNumber) = MusicXmlNoteParser.ParseNote(element, context);
                    var voice = voiceNumber ?? defaultVoice;

                    if (!voiceEvents.TryGetValue(voice, out var events))
                    {
                        events = new List<INotationEvent>();
                        voiceEvents[voice] = events;
                    }
                    events.Add(noteEvent);
                    break;

                case "direction":
                    // Parse directions (tempo, dynamics, text, etc.)
                    var parsedDirections = MusicXmlDirectionParser.ParseDirection(element);
                    directions.AddRange(parsedDirections);
                    break;

                case "barline":
                    // Parse barline (repeats, endings)
                    var location = element.Attribute("location")?.Value;
                    var (barlineType, variants) = MusicXmlBarlineParser.ParseBarline(element);

                    if (location == "left")
                    {
                        // Left barline starts the measure
                        startBarline = barlineType;
                        if (variants != null)
                        {
                            repeatVariants = variants;
                        }
                    }
                    else // "right" or no location (defaults to right)
                    {
                        // Right barline ends the measure
                        endBarline = barlineType;
                        if (variants != null && repeatVariants == null)
                        {
                            repeatVariants = variants;
                        }
                    }
                    break;

                case "backup":
                case "forward":
                    // TODO: Handle backup/forward for multi-voice
                    break;
            }
        }

        // Create measures for each voice
        var measures = new Dictionary<int, Measure>();
        foreach (var (voiceNumber, events) in voiceEvents)
        {
            measures[voiceNumber] = new Measure(
                measureNumber,
                events,
                repeatVariants: repeatVariants,
                startBarline: startBarline,
                endBarline: endBarline,
                directions: directions.Count > 0 ? directions : null);
        }

        // If no voices have events, create an empty measure for voice 1
        if (measures.Count == 0)
        {
            measures[defaultVoice] = new Measure(
                measureNumber,
                new List<INotationEvent>(),
                repeatVariants: repeatVariants,
                startBarline: startBarline,
                endBarline: endBarline,
                directions: directions.Count > 0 ? directions : null);
        }

        return measures;
    }
}
