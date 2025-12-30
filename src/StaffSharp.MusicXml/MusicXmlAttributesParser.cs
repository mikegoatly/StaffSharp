namespace StaffSharp.MusicXml;

using StaffSharp;
using StaffSharp.Notation;
using System.Xml.Linq;

/// <summary>
/// Parses MusicXML attributes elements (divisions, key, time, clef).
/// </summary>
internal static class MusicXmlAttributesParser
{
    /// <summary>
    /// Parses an attributes element and updates the context.
    /// </summary>
    public static void ParseAttributes(XElement attributesElement, MusicXmlContext context)
    {
        ArgumentNullException.ThrowIfNull(attributesElement);
        ArgumentNullException.ThrowIfNull(context);

        // Parse divisions (ticks per quarter note)
        var divisionsElement = attributesElement.Element("divisions");
        if (divisionsElement != null && int.TryParse(divisionsElement.Value, out var divisions))
        {
            context.Divisions = divisions;
        }

        // Parse key signature
        var keyElement = attributesElement.Element("key");
        if (keyElement != null)
        {
            context.KeySignature = ParseKeySignature(keyElement);
        }

        // Parse time signature
        var timeElement = attributesElement.Element("time");
        if (timeElement != null)
        {
            context.TimeSignature = ParseTimeSignature(timeElement);
        }

        // Parse clef
        var clefElement = attributesElement.Element("clef");
        if (clefElement != null)
        {
            context.Clef = ParseClef(clefElement);
        }
    }

    private static KeySignature ParseKeySignature(XElement keyElement)
    {
        var fifthsElement = keyElement.Element("fifths");
        if (fifthsElement == null || !int.TryParse(fifthsElement.Value, out var fifths))
        {
            return KeySignature.C;
        }

        // Map fifths to KeySignature
        // Positive = sharps, negative = flats
        return fifths switch
        {
            -7 => KeySignature.CFlat,
            -6 => KeySignature.GFlat,
            -5 => KeySignature.DFlat,
            -4 => KeySignature.AFlat,
            -3 => KeySignature.EFlat,
            -2 => KeySignature.BFlat,
            -1 => KeySignature.F,
            0 => KeySignature.C,
            1 => KeySignature.G,
            2 => KeySignature.D,
            3 => KeySignature.A,
            4 => KeySignature.E,
            5 => KeySignature.B,
            6 => KeySignature.FSharp,
            7 => KeySignature.CSharp,
            _ => KeySignature.C
        };
    }

    private static TimeSignature ParseTimeSignature(XElement timeElement)
    {
        var beatsElement = timeElement.Element("beats");
        var beatTypeElement = timeElement.Element("beat-type");

        if (beatsElement != null && beatTypeElement != null &&
            int.TryParse(beatsElement.Value, out var beats) &&
            int.TryParse(beatTypeElement.Value, out var beatType))
        {
            return new TimeSignature(beats, beatType);
        }

        return TimeSignature.CommonTime;
    }

    private static Clef ParseClef(XElement clefElement)
    {
        var signElement = clefElement.Element("sign");
        var lineElement = clefElement.Element("line");

        if (signElement == null)
        {
            return Clef.Treble;
        }

        var sign = signElement.Value;
        var line = lineElement != null && int.TryParse(lineElement.Value, out var l) ? l : 0;

        return (sign, line) switch
        {
            ("G", 2) => Clef.Treble,
            ("F", 4) => Clef.Bass,
            ("C", 3) => Clef.Alto,
            ("C", 4) => Clef.Tenor,
            _ => Clef.Treble
        };
    }
}
