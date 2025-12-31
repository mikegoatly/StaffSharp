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

        // Track slurs per voice: voice -> (slur number -> start event index)
        var activeSlurs = new Dictionary<int, Dictionary<int, int>>();
        // Completed slurs per voice
        var voiceSlurs = new Dictionary<int, List<Slur>>();
        // Track lyrics per voice: voice -> (lyric number -> syllables)
        var voiceLyrics = new Dictionary<int, Dictionary<int, List<LyricSyllable>>>();

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
                    var (noteEvent, voiceNumber, slurInfos, lyricSyllables) = MusicXmlNoteParser.ParseNote(element, context);
                    var voice = voiceNumber ?? defaultVoice;

                    if (!voiceEvents.TryGetValue(voice, out var events))
                    {
                        events = new List<INotationEvent>();
                        voiceEvents[voice] = events;
                    }
                    events.Add(noteEvent);

                    // Process slur information
                    if (slurInfos != null)
                    {
                        if (!activeSlurs.TryGetValue(voice, out var slurStarts))
                        {
                            slurStarts = new Dictionary<int, int>();
                            activeSlurs[voice] = slurStarts;
                        }

                        if (!voiceSlurs.TryGetValue(voice, out var slurs))
                        {
                            slurs = new List<Slur>();
                            voiceSlurs[voice] = slurs;
                        }

                        foreach (var slurInfo in slurInfos)
                        {
                            if (slurInfo.Type == SlurType.Start)
                            {
                                // Record the start index (current event)
                                slurStarts[slurInfo.Number] = events.Count - 1;
                            }
                            else if (slurInfo.Type == SlurType.Stop)
                            {
                                // Find the start index and create a slur
                                if (slurStarts.TryGetValue(slurInfo.Number, out var startIndex))
                                {
                                    var slurEvents = new List<INotationEvent>();
                                    for (int i = startIndex; i < events.Count; i++)
                                    {
                                        slurEvents.Add(events[i]);
                                    }

                                    if (slurEvents.Count >= 2)
                                    {
                                        slurs.Add(new Slur(slurEvents));
                                    }

                                    slurStarts.Remove(slurInfo.Number);
                                }
                            }
                        }
                    }

                    // Process lyric syllables
                    if (lyricSyllables != null)
                    {
                        if (!voiceLyrics.TryGetValue(voice, out var lyricLines))
                        {
                            lyricLines = new Dictionary<int, List<LyricSyllable>>();
                            voiceLyrics[voice] = lyricLines;
                        }

                        foreach (var (lyricNumber, syllable) in lyricSyllables)
                        {
                            if (!lyricLines.TryGetValue(lyricNumber, out var syllables))
                            {
                                syllables = new List<LyricSyllable>();
                                lyricLines[lyricNumber] = syllables;
                            }
                            syllables.Add(syllable);
                        }
                    }
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
            voiceSlurs.TryGetValue(voiceNumber, out var slurs);

            // Create lyrics list from lyric lines
            List<Lyric>? lyrics = null;
            if (voiceLyrics.TryGetValue(voiceNumber, out var lyricLines))
            {
                lyrics = new List<Lyric>();
                foreach (var syllables in lyricLines.Values)
                {
                    lyrics.Add(new Lyric(syllables));
                }
            }

            measures[voiceNumber] = new Measure(
                measureNumber,
                events,
                repeatVariants: repeatVariants,
                slurs: slurs,
                lyrics: lyrics,
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
