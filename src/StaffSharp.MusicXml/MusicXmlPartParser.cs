namespace StaffSharp.MusicXml;

using StaffSharp.Notation;
using System.Xml.Linq;

/// <summary>
/// Parses a MusicXML part element into staves.
/// </summary>
internal static class MusicXmlPartParser
{
    /// <summary>
    /// Parses a part element and returns a list of staves.
    /// </summary>
    public static List<Staff> ParsePart(XElement partElement, MusicXmlContext context)
    {
        // Group measures by staff -> voice -> list of measures
        var staffData = new Dictionary<int, Dictionary<int, List<Measure>>>();

        int measureNumber = 1;
        foreach (var measureElement in partElement.Elements("measure"))
        {
            // Parse measure and group events by staff and voice
            var staffVoiceMeasures = MusicXmlMeasureParser.ParseMeasure(measureElement, context, measureNumber);

            // Merge into the staff data structure
            foreach (var (staffNumber, voiceMeasures) in staffVoiceMeasures)
            {
                if (!staffData.TryGetValue(staffNumber, out var voiceData))
                {
                    voiceData = [];
                    staffData[staffNumber] = voiceData;
                }

                foreach (var (voiceNumber, measure) in voiceMeasures)
                {
                    if (!voiceData.TryGetValue(voiceNumber, out var measures))
                    {
                        measures = [];
                        voiceData[voiceNumber] = measures;
                    }

                    measures.Add(measure);
                }
            }

            measureNumber++;
        }

        // Create Staff objects
        var staves = new List<Staff>();
        foreach (var staffNumber in staffData.Keys.OrderBy(n => n))
        {
            var voiceData = staffData[staffNumber];

            // Create Voice objects for this staff
            var voices = new List<Voice>();
            foreach (var (voiceNumber, measures) in voiceData.OrderBy(kvp => kvp.Key))
            {
                voices.Add(new Voice(voiceNumber, measures));
            }

            // If no voices were created for this staff, create an empty voice 1
            if (voices.Count == 0)
            {
                voices.Add(new Voice(1, []));
            }

            // Get the clef for this staff
            var clef = context.GetClefForStaff(staffNumber);

            staves.Add(
                new Staff(
                    number: staffNumber,
                    clef: clef,
                    voices: voices
                ));
        }

        // If no staves were created, create a default staff 1 with empty voice 1
        if (staves.Count == 0)
        {
            staves.Add(
                new Staff(
                    number: 1,
                    clef: context.Clef,
                    voices: [new Voice(1, [])]
                ));
        }

        return staves;
    }
}
