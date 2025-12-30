namespace StaffSharp.MusicXml;

using StaffSharp.Notation;
using System.Xml.Linq;

/// <summary>
/// Parses MusicXML note elements.
/// </summary>
internal static class MusicXmlNoteParser
{
    /// <summary>
    /// Parses a note element and returns the event and voice number.
    /// </summary>
    public static (INotationEvent Event, int? VoiceNumber) ParseNote(XElement noteElement, MusicXmlContext context)
    {
        ArgumentNullException.ThrowIfNull(noteElement);
        ArgumentNullException.ThrowIfNull(context);

        // Check if this is a chord (simultaneous with previous note)
        bool isChord = noteElement.Element("chord") != null;

        // Get voice number
        var voiceElement = noteElement.Element("voice");
        int? voiceNumber = voiceElement != null && int.TryParse(voiceElement.Value, out var v) ? v : null;

        // Check if it's a rest
        var restElement = noteElement.Element("rest");
        if (restElement != null)
        {
            return (ParseRest(noteElement, context), voiceNumber);
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

        return (note, voiceNumber);
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
}
