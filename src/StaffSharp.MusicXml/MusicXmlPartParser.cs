namespace StaffSharp.MusicXml;

using StaffSharp.Notation;
using System.Xml.Linq;

/// <summary>
/// Parses a MusicXML part element into voices.
/// </summary>
internal static class MusicXmlPartParser
{
    /// <summary>
    /// Parses a part element and returns a list of voices.
    /// </summary>
    public static List<Voice> ParsePart(XElement partElement, MusicXmlContext context)
    {
        ArgumentNullException.ThrowIfNull(partElement);
        ArgumentNullException.ThrowIfNull(context);

        // Group measures by voice
        var voiceData = new Dictionary<int, List<Measure>>();

        int measureNumber = 1;
        foreach (var measureElement in partElement.Elements("measure"))
        {
            // Parse measure and group events by voice
            var voiceMeasures = MusicXmlMeasureParser.ParseMeasure(measureElement, context, measureNumber);

            // Merge voice measures into the voice data
            foreach (var (voiceNumber, measure) in voiceMeasures)
            {
                if (!voiceData.TryGetValue(voiceNumber, out var measures))
                {
                    measures = new List<Measure>();
                    voiceData[voiceNumber] = measures;
                }
                measures.Add(measure);
            }

            measureNumber++;
        }

        // Create Voice objects
        var voices = new List<Voice>();
        foreach (var (voiceNumber, measures) in voiceData.OrderBy(kvp => kvp.Key))
        {
            voices.Add(new Voice(voiceNumber, measures));
        }

        // If no voices were created, create an empty voice 1
        if (voices.Count == 0)
        {
            voices.Add(new Voice(1, new List<Measure>()));
        }

        return voices;
    }
}
