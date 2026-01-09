namespace StaffSharp.MusicXml;

using StaffSharp.Notation;
using System.Xml.Linq;

/// <summary>
/// Parses a MusicXML measure element.
/// </summary>
internal static class MusicXmlMeasureParser
{
    /// <summary>
    /// Parses a measure element and returns measures grouped by staff number and voice number.
    /// Returns: Dictionary&lt;staffNumber, Dictionary&lt;voiceNumber, Measure&gt;&gt;
    /// </summary>
    public static Dictionary<int, Dictionary<int, Measure>> ParseMeasure(XElement measureElement, MusicXmlContext context, int measureNumber, SlurSpanAggregator? slurAggregator = null)
    {
        ArgumentNullException.ThrowIfNull(measureElement);
        ArgumentNullException.ThrowIfNull(context);

        const int defaultVoice = 1;
        const int defaultStaff = 1;
        var directions = new List<Direction>();
        BarlineType? startBarline = null;
        BarlineType? endBarline = null;
        List<int>? repeatVariants = null;

        // Track state per (staff, voice) pair
        var staffVoiceStates = new Dictionary<int, Dictionary<int, VoiceState>>();

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
                    // Parse note and add to appropriate staff and voice
                    var (noteEvent, voiceNumber, staffNumber, slurInfos, lyricSyllables) = MusicXmlNoteParser.ParseNote(element, context);
                    var voice = voiceNumber ?? defaultVoice;
                    var staff = staffNumber; // Already has default value of 1

                    // Get or create voice states for this staff
                    if (!staffVoiceStates.TryGetValue(staff, out var voiceStates))
                    {
                        voiceStates = new Dictionary<int, VoiceState>();
                        staffVoiceStates[staff] = voiceStates;
                    }

                    // Get or create voice state for this voice within the staff
                    if (!voiceStates.TryGetValue(voice, out var voiceState))
                    {
                        voiceState = new VoiceState();
                        voiceStates[voice] = voiceState;
                    }

                    voiceState.Events.Add(noteEvent);

                    // Process slur information: populate legacy measure-level slurs and forward to aggregator
                    if (slurInfos != null)
                    {
                        // legacy per-measure grouping
                        foreach (var slurInfo in slurInfos)
                        {
                            if (slurInfo.Type == SlurType.Start)
                            {
                                voiceState.ActiveSlurs[slurInfo.Number] = voiceState.Events.Count - 1;
                            }
                            else if (slurInfo.Type == SlurType.Stop)
                            {
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

                        // aggregator for cross-measure spans
                        if (slurAggregator != null)
                        {
                            slurAggregator.OnNote(noteEvent, staff, voice, slurInfos);
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

        // Create measures grouped by staff and voice
        var staffMeasures = new Dictionary<int, Dictionary<int, Measure>>();

        if (staffVoiceStates.Count > 0)
        {
            foreach (var (staffNumber, voiceStates) in staffVoiceStates)
            {
                var measures = new Dictionary<int, Measure>();
                foreach (var (voiceNumber, state) in voiceStates)
                {
                    measures[voiceNumber] = CreateMeasure(state, measureNumber, repeatVariants, startBarline, endBarline, directions);
                }
                staffMeasures[staffNumber] = measures;
            }
        }
        else
        {
            // No staves/voices have events, create an empty measure for staff 1, voice 1
            var emptyMeasures = new Dictionary<int, Measure>
            {
                [defaultVoice] = new Measure(
                    measureNumber,
                    [],
                    repeatVariants: repeatVariants,
                    startBarline: startBarline,
                    endBarline: endBarline,
                    directions: directions.Count > 0 ? directions : null)
            };
            staffMeasures[defaultStaff] = emptyMeasures;
        }

        return staffMeasures;
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
        // Legacy fields kept for compatibility; no longer populated for slurs
        public Dictionary<int, int> ActiveSlurs { get; } = []; // slur number -> start event index
        public List<Slur> Slurs { get; } = [];
        public Dictionary<int, List<LyricSyllable>> Lyrics { get; } = []; // lyric number -> syllables
    }
}
