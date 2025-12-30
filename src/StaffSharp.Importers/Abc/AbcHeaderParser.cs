namespace StaffSharp.Importers.Abc;

using StaffSharp;
using StaffSharp.Notation;

/// <summary>
/// Parses ABC notation header fields.
/// </summary>
internal static class AbcHeaderParser
{
    public static TimeSignature ParseTimeSignature(string value)
    {
        value = value.Trim();

        if (value == "C")
        {
            return TimeSignature.CommonTime;
        }

        if (value == "C|")
        {
            return new TimeSignature(2, 2); // Cut time
        }

        var parts = value.Split('/');
        if (parts.Length == 2 && int.TryParse(parts[0], out var numerator) && int.TryParse(parts[1], out var denominator))
        {
            return new TimeSignature(numerator, denominator);
        }

        return TimeSignature.CommonTime;
    }

    public static Rational ParseDefaultNoteLength(string value)
    {
        // Format: 1/4, 1/8, etc.
        // This represents the note duration, which needs to be converted to beats (quarter notes)
        var parts = value.Trim().Split('/');
        if (parts.Length == 2 && int.TryParse(parts[0], out var numerator) && int.TryParse(parts[1], out var denominator))
        {
            // Convert note duration to beats: a 1/8 note = 4/8 = 1/2 beat
            return Rational.Create(numerator * 4, denominator);
        }

        return Rational.Create(1, 2); // Default: 1/8 note = 1/2 beat
    }

    public static KeySignature ParseKeySignature(string value)
    {
        // Remove mode suffixes (major, minor, etc.) for now - just look at root
        var key = value.Trim()
            .Replace("major", "", StringComparison.OrdinalIgnoreCase)
            .Replace("minor", "", StringComparison.OrdinalIgnoreCase)
            .Replace("min", "", StringComparison.OrdinalIgnoreCase)
            .Replace("m", "", StringComparison.OrdinalIgnoreCase)
            .Trim()
            .ToUpperInvariant();

        return key switch
        {
            "C" => KeySignature.C,
            "G" => KeySignature.G,
            "D" => KeySignature.D,
            "A" => KeySignature.A,
            "E" => KeySignature.E,
            "B" => KeySignature.B,
            "F#" or "FSHARP" => KeySignature.FSharp,
            "C#" or "CSHARP" => KeySignature.CSharp,
            "F" => KeySignature.F,
            "BB" or "BFLAT" => KeySignature.BFlat,
            "EB" or "EFLAT" => KeySignature.EFlat,
            "AB" or "AFLAT" => KeySignature.AFlat,
            "DB" or "DFLAT" => KeySignature.DFlat,
            "GB" or "GFLAT" => KeySignature.GFlat,
            "CB" or "CFLAT" => KeySignature.CFlat,
            _ => KeySignature.C
        };
    }

    public static int ParseTempo(string value)
    {
        // ABC tempo format: "1/4=120" or just "120"
        // Extract the number after the '=' if present, otherwise use the last number
        var match = System.Text.RegularExpressions.Regex.Match(value, @"=\s*(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var tempo))
        {
            return tempo;
        }

        // Fallback: try to find any number
        match = System.Text.RegularExpressions.Regex.Match(value, @"\d+");
        return match.Success && int.TryParse(match.Value, out tempo) ? tempo : 120;
    }
}
