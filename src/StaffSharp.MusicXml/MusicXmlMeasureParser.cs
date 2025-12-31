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

        const int defaultVoice = 1;
        var directions = new List<Direction>();
        BarlineType? startBarline = null;
        BarlineType? endBarline = null;
        List<int>? repeatVariants = null;

        // Track state per voice
        var voiceStates = new Dictionary<int, VoiceState>();

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

                    if (!voiceStates.TryGetValue(voice, out var voiceState))
                    {
                        voiceState = new VoiceState();
                        voiceStates[voice] = voiceState;
                    }

                    voiceState.Events.Add(noteEvent);

                    // Process slur information
                    if (slurInfos != null)
                    {
                        foreach (var slurInfo in slurInfos)
                        {
                            if (slurInfo.Type == SlurType.Start)
                            {
                                // Record the start index (current event)
                                voiceState.ActiveSlurs[slurInfo.Number] = voiceState.Events.Count - 1;
                            }
                            else if (slurInfo.Type == SlurType.Stop)
                            {
                                // Find the start index and create a slur
                                if (voiceState.ActiveSlurs.TryGetValue(slurInfo.Number, out var startIndex))
                                {
                                    var slurEvents = new List<INotationEvent>();
                                    for (int i = startIndex; i < voiceState.Events.Count; i++)
                                    {
                                        slurEvents.Add(voiceState.Events[i]);
                                    }

                                    if (slurEvents.Count >= 2)
                                    {
                                        voiceState.Slurs.Add(new Slur(slurEvents));
                                    }

                                    voiceState.ActiveSlurs.Remove(slurInfo.Number);
                                }
                            }
                        }
                    }

                    // Process lyric syllables
                    if (lyricSyllables != null)
                    {
                        foreach (var (lyricNumber, syllable) in lyricSyllables)
                        {
                            if (!voiceState.Lyrics.TryGetValue(lyricNumber, out var syllables))
                            {
                                syllables = [];
                                voiceState.Lyrics[lyricNumber] = syllables;
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

        // Create measures from voice states
        var measures = new Dictionary<int, Measure>();

        if (voiceStates.Count > 0)
        {
            foreach (var (voiceNumber, state) in voiceStates)
            {
                measures[voiceNumber] = CreateMeasure(state, measureNumber, repeatVariants, startBarline, endBarline, directions);
            }
        }
        else
        {
            // No voices have events, create an empty measure for voice 1
            measures[defaultVoice] = new Measure(
                measureNumber,
                [],
                repeatVariants: repeatVariants,
                startBarline: startBarline,
                endBarline: endBarline,
                directions: directions.Count > 0 ? directions : null);
        }

        return measures;
    }

    private static Measure CreateMeasure(
        VoiceState state,
        int measureNumber,
        List<int>? repeatVariants,
        BarlineType? startBarline,
        BarlineType? endBarline,
        List<Direction> directions)
    {
        // Create lyrics list from lyric lines
        List<Lyric>? lyrics = null;
        if (state.Lyrics.Count > 0)
        {
            lyrics = new List<Lyric>(state.Lyrics.Count);
            foreach (var syllables in state.Lyrics.Values)
            {
                lyrics.Add(new Lyric(syllables));
            }
        }

        return new Measure(
            measureNumber,
            state.Events,
            repeatVariants: repeatVariants,
            slurs: state.Slurs.Count > 0 ? state.Slurs : null,
            lyrics: lyrics,
            startBarline: startBarline,
            endBarline: endBarline,
            directions: directions.Count > 0 ? directions : null);
    }

    /// <summary>
    /// Holds all parsing state for a single voice within a measure.
    /// </summary>
    private sealed class VoiceState
    {
        public List<INotationEvent> Events { get; } = [];
        public Dictionary<int, int> ActiveSlurs { get; } = []; // slur number -> start event index
        public List<Slur> Slurs { get; } = [];
        public Dictionary<int, List<LyricSyllable>> Lyrics { get; } = []; // lyric number -> syllables
    }
}
