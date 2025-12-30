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
                    // TODO: Parse directions (tempo, dynamics)
                    break;

                case "barline":
                    // TODO: Parse barlines (repeats)
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
            measures[voiceNumber] = new Measure(measureNumber, events);
        }

        // If no voices have events, create an empty measure for voice 1
        if (measures.Count == 0)
        {
            measures[defaultVoice] = new Measure(measureNumber, new List<INotationEvent>());
        }

        return measures;
    }
}
