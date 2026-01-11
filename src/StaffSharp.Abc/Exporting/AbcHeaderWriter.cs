namespace StaffSharp.Abc.Exporting;

using System.Globalization;
using System.Text;

using StaffSharp;
using StaffSharp.Notation;

/// <summary>
/// Writes ABC notation header fields.
/// </summary>
internal static class AbcHeaderWriter
{
    /// <summary>
    /// Writes ABC headers to a StringBuilder.
    /// </summary>
    /// <param name="sb">The StringBuilder to append to.</param>
    /// <param name="metadata">The score metadata.</param>
    /// <param name="options">Export options.</param>
    /// <remarks>
    /// Headers are written in this order (ABC standard):
    /// X: (reference number)
    /// T: (title)
    /// C: (composer)
    /// M: (time signature)
    /// L: (default note length)
    /// Q: (tempo)
    /// K: (key signature) - must be last
    /// </remarks>
    public static void WriteHeaders(StringBuilder sb, ScoreMetadata metadata, AbcExportOptions options)
    {
        // X: Reference number (always 1 for single tunes)
        sb.AppendLine("X:1");

        // T: Title
        if (!string.IsNullOrWhiteSpace(metadata.Title))
        {
            sb.Append(CultureInfo.InvariantCulture, $"T:{metadata.Title}");
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("T:Untitled");
        }

        // C: Composer
        if (!string.IsNullOrWhiteSpace(metadata.Composer))
        {
            sb.Append(CultureInfo.InvariantCulture, $"C:{metadata.Composer}");
            sb.AppendLine();
        }

        // M: Time signature
        sb.Append(CultureInfo.InvariantCulture, $"M:{FormatTimeSignature(metadata.TimeSignature)}");
        sb.AppendLine();

        // L: Default note length
        sb.Append(CultureInfo.InvariantCulture, $"L:{FormatDefaultNoteLength(options.DefaultNoteLength)}");
        sb.AppendLine();

        // Q: Tempo
        sb.Append(CultureInfo.InvariantCulture, $"Q:{metadata.Tempo.ToString(CultureInfo.InvariantCulture)}");
        sb.AppendLine();

        // K: Key signature (must be last header field)
        sb.Append(CultureInfo.InvariantCulture, $"K:{FormatKeySignature(metadata.KeySignature)}");
        sb.AppendLine();
    }

    private static string FormatTimeSignature(TimeSignature timeSignature)
    {
        // Special cases
        if (timeSignature.Numerator == 4 && timeSignature.Denominator == 4)
        {
            return "C"; // Common time
        }

        if (timeSignature.Numerator == 2 && timeSignature.Denominator == 2)
        {
            return "C|"; // Cut time
        }

        // Standard format: 3/4, 6/8, etc.
        return $"{timeSignature.Numerator.ToString(CultureInfo.InvariantCulture)}/{timeSignature.Denominator.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string FormatDefaultNoteLength(Rational noteLength)
    {
        // Note length is in "note duration" form (1/8, 1/4, etc.)
        return $"{noteLength.Numerator.ToString(CultureInfo.InvariantCulture)}/{noteLength.Denominator.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string FormatKeySignature(KeySignature keySignature)
    {
        // Map KeySignature to ABC notation based on number of sharps/flats
        return keySignature.Sharps switch
        {
            0 => "C",      // C major
            1 => "G",      // G major
            2 => "D",      // D major
            3 => "A",      // A major
            4 => "E",      // E major
            5 => "B",      // B major
            6 => "F#",     // F# major
            7 => "C#",     // C# major
            -1 => "F",     // F major
            -2 => "Bb",    // Bb major
            -3 => "Eb",    // Eb major
            -4 => "Ab",    // Ab major
            -5 => "Db",    // Db major
            -6 => "Gb",    // Gb major
            -7 => "Cb",    // Cb major
            _ => "C"       // Default to C major
        };
    }
}
