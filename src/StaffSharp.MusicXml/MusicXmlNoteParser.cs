namespace StaffSharp.MusicXml;

using StaffSharp.Notation;
using System.Xml.Linq;

/// <summary>
/// Parses MusicXML note elements.
/// </summary>
internal static class MusicXmlNoteParser
{
    /// <summary>
    /// Parses a note element and returns the event, voice number, slur information, and lyric syllables.
    /// </summary>
    public static (INotationEvent Event, int? VoiceNumber, List<SlurInfo>? SlurInfos, Dictionary<int, LyricSyllable>? LyricSyllables) ParseNote(XElement noteElement, MusicXmlContext context)
    {
        ArgumentNullException.ThrowIfNull(noteElement);
        ArgumentNullException.ThrowIfNull(context);

        // Check if this is a chord (simultaneous with previous note)
        bool isChord = noteElement.Element("chord") != null;

        // Get voice number
        var voiceElement = noteElement.Element("voice");
        int? voiceNumber = voiceElement != null && int.TryParse(voiceElement.Value, out var v) ? v : null;

        // Parse lyrics
        var lyricSyllables = ParseLyrics(noteElement);

        // Check if it's a rest
        var restElement = noteElement.Element("rest");
        if (restElement != null)
        {
            var slurInfosForRest = ParseSlurs(noteElement);
            return (ParseRest(noteElement, context), voiceNumber, slurInfosForRest, lyricSyllables);
        }

        // Parse pitch
        var pitchElement = noteElement.Element("pitch");
        if (pitchElement == null)
        {
            throw new InvalidOperationException("Note element is missing pitch.");
        }

        var pitch = ParsePitch(pitchElement);

        // Parse duration
        var durationElement = noteElement.Element("duration");
        if (durationElement == null || !int.TryParse(durationElement.Value, out var durationValue))
        {
            throw new InvalidOperationException("Note element is missing or has invalid duration.");
        }

        // Check for tuplet (time-modification)
        Tuplet? tuplet = null;
        var timeModElement = noteElement.Element("time-modification");
        if (timeModElement != null)
        {
            tuplet = ParseTuplet(timeModElement);
        }

        var duration = DurationConverter.Convert(durationValue, context.Divisions, tuplet);

        // Create note
        var note = new NotationNote(pitch, duration);

        // Parse slurs
        var slurInfos = ParseSlurs(noteElement);

        return (note, voiceNumber, slurInfos, lyricSyllables);
    }

    private static Pitch ParsePitch(XElement pitchElement)
    {
        var stepElement = pitchElement.Element("step");
        var octaveElement = pitchElement.Element("octave");

        if (stepElement == null || octaveElement == null ||
            !int.TryParse(octaveElement.Value, out var octave))
        {
            throw new InvalidOperationException("Pitch element is missing step or octave.");
        }

        var step = stepElement.Value;
        var pitchClass = step switch
        {
            "C" => PitchClass.C,
            "D" => PitchClass.D,
            "E" => PitchClass.E,
            "F" => PitchClass.F,
            "G" => PitchClass.G,
            "A" => PitchClass.A,
            "B" => PitchClass.B,
            _ => throw new InvalidOperationException($"Invalid pitch step: {step}")
        };

        // Check for alter (accidental)
        Accidental accidental = Accidental.Natural;
        var alterElement = pitchElement.Element("alter");
        if (alterElement != null && int.TryParse(alterElement.Value, out var alter))
        {
            accidental = alter switch
            {
                -2 => Accidental.DoubleFlat,
                -1 => Accidental.Flat,
                0 => Accidental.Natural,
                1 => Accidental.Sharp,
                2 => Accidental.DoubleSharp,
                _ => Accidental.Natural
            };
        }

        return new Pitch(pitchClass, octave, accidental);
    }

    private static Rest ParseRest(XElement noteElement, MusicXmlContext context)
    {
        // Parse duration
        var durationElement = noteElement.Element("duration");
        if (durationElement == null || !int.TryParse(durationElement.Value, out var durationValue))
        {
            throw new InvalidOperationException("Rest element is missing or has invalid duration.");
        }

        var duration = DurationConverter.Convert(durationValue, context.Divisions);

        return new Rest(duration);
    }

    private static Tuplet ParseTuplet(XElement timeModElement)
    {
        var actualNotesElement = timeModElement.Element("actual-notes");
        var normalNotesElement = timeModElement.Element("normal-notes");

        if (actualNotesElement != null && normalNotesElement != null &&
            int.TryParse(actualNotesElement.Value, out var actualNotes) &&
            int.TryParse(normalNotesElement.Value, out var normalNotes))
        {
            return new Tuplet(actualNotes, normalNotes);
        }

        return Tuplet.Triplet; // Default fallback
    }

    private static List<SlurInfo>? ParseSlurs(XElement noteElement)
    {
        var notationsElement = noteElement.Element("notations");
        if (notationsElement == null)
        {
            return null;
        }

        var slurInfos = new List<SlurInfo>();
        foreach (var slurElement in notationsElement.Elements("slur"))
        {
            var typeAttr = slurElement.Attribute("type");
            var numberAttr = slurElement.Attribute("number");

            if (typeAttr != null && numberAttr != null && int.TryParse(numberAttr.Value, out var number))
            {
                var type = typeAttr.Value switch
                {
                    "start" => SlurType.Start,
                    "stop" => SlurType.Stop,
                    _ => (SlurType?)null
                };

                if (type.HasValue)
                {
                    slurInfos.Add(new SlurInfo(number, type.Value));
                }
            }
        }

        return slurInfos.Count > 0 ? slurInfos : null;
    }

    private static Dictionary<int, LyricSyllable>? ParseLyrics(XElement noteElement)
    {
        var lyricElements = noteElement.Elements("lyric").ToList();
        if (lyricElements.Count == 0)
        {
            return null;
        }

        var syllables = new Dictionary<int, LyricSyllable>();

        foreach (var lyricElement in lyricElements)
        {
            var numberAttr = lyricElement.Attribute("number");
            var lyricNumber = numberAttr != null && int.TryParse(numberAttr.Value, out var num) ? num : 1;

            var textElement = lyricElement.Element("text");
            if (textElement == null)
            {
                continue;
            }

            var text = textElement.Value;

            // Determine syllable type
            var syllabicElement = lyricElement.Element("syllabic");
            var syllableType = LyricSyllableType.Standalone;

            if (syllabicElement != null)
            {
                syllableType = syllabicElement.Value switch
                {
                    "single" => LyricSyllableType.Standalone,
                    "begin" => LyricSyllableType.Start,
                    "middle" => LyricSyllableType.Middle,
                    "end" => LyricSyllableType.End,
                    _ => LyricSyllableType.Standalone
                };
            }

            // Check for extend (melisma/hold)
            var extendElement = lyricElement.Element("extend");
            if (extendElement != null)
            {
                syllableType = LyricSyllableType.Hold;
            }

            syllables[lyricNumber] = new LyricSyllable(text, syllableType);
        }

        return syllables.Count > 0 ? syllables : null;
    }
}

/// <summary>
/// Information about a slur start or stop.
/// </summary>
internal record SlurInfo(int Number, SlurType Type);

/// <summary>
/// Type of slur marking.
/// </summary>
internal enum SlurType
{
    Start,
    Stop
}
