namespace StaffSharp.Importers.Abc;

using StaffSharp.Notation;

/// <summary>
/// Parses ABC notation decorations (!trill!, ., ~, etc.).
/// Decorations apply to the next note/chord.
/// </summary>
internal static class AbcDecorationParser
{
    /// <summary>
    /// Tries to parse decoration(s) at the current position.
    /// Returns all consecutive decorations found.
    /// </summary>
    public static List<Decoration> ParseDecorations(string input, ref int index)
    {
        var decorations = new List<Decoration>();

        while (index < input.Length)
        {
            // Skip whitespace
            while (index < input.Length && char.IsWhiteSpace(input[index]))
            {
                index++;
            }

            if (index >= input.Length)
            {
                break;
            }

            // Try named decoration !name!
            if (input[index] == '!')
            {
                if (TryParseNamedDecoration(input, ref index, out var decoration))
                {
                    decorations.Add(decoration);
                    continue;
                }
            }

            // Try shorthand decoration (single character)
            if (TryParseShorthandDecoration(input, ref index, out var shorthand))
            {
                decorations.Add(shorthand);
                continue;
            }

            // Not a decoration
            break;
        }

        return decorations;
    }

    private static bool TryParseNamedDecoration(string input, ref int index, out Decoration decoration)
    {
        decoration = default;

        if (index >= input.Length || input[index] != '!')
        {
            return false;
        }

        var startIndex = index;
        index++; // Skip opening !

        // Find closing !
        int nameStart = index;
        while (index < input.Length && input[index] != '!')
        {
            index++;
        }

        if (index >= input.Length)
        {
            // No closing !, reset
            index = startIndex;
            return false;
        }

        var name = input[nameStart..index].ToUpperInvariant();
        index++; // Skip closing !

        // Map name to decoration
        var result = name switch
        {
            // Ornaments
            "TRILL" => Decoration.Trill,
            "MORDENT" => Decoration.Mordent,
            "LOWERMORDENT" => Decoration.LowerMordent,
            "UPPERMORDENT" => Decoration.UpperMordent,
            "TURN" => Decoration.Turn,
            "INVERTEDTURN" => Decoration.InvertedTurn,

            // Holds
            "FERMATA" => Decoration.Fermata,
            "BREATH" => Decoration.Breath,

            // Bowing
            "UPBOW" => Decoration.UpBow,
            "DOWNBOW" => Decoration.DownBow,

            // Dynamics
            "PP" or "PIANISSIMO" => Decoration.Pianissimo,
            "P" or "PIANO" => Decoration.Piano,
            "MP" or "MEZZOPIANO" => Decoration.MezzoPiano,
            "MF" or "MEZZOFORTE" => Decoration.MezzoForte,
            "F" or "FORTE" => Decoration.Forte,
            "FF" or "FORTISSIMO" => Decoration.Fortissimo,
            "SFZ" or "SFORZANDO" => Decoration.Sforzando,
            "CRESCENDO" or "<(" => Decoration.Crescendo,
            "DIMINUENDO" or ">(" or "DECRESCENDO" => Decoration.Diminuendo,

            // Pedal
            "PEDAL" => Decoration.Pedal,
            "PEDAL-UP" => Decoration.PedalUp,

            _ => (Decoration?)null
        };

        if (result == null)
        {
            // Unknown decoration, reset
            index = startIndex;
            return false;
        }

        decoration = result.Value;
        return true;
    }

    private static bool TryParseShorthandDecoration(string input, ref int index, out Decoration decoration)
    {
        decoration = default;

        if (index >= input.Length)
        {
            return false;
        }

        char c = input[index];

        // Special case: Don't treat '.' as staccato if followed by '(' (dotted slur)
        if (c == '.' && index + 1 < input.Length && input[index + 1] == '(')
        {
            return false;
        }

        var result = c switch
        {
            '.' => Decoration.Staccato,
            '~' => Decoration.Roll,
            'T' => Decoration.Trill,
            'M' => Decoration.Mordent,
            'H' => Decoration.Fermata,
            'L' => Decoration.Accent,
            'u' => Decoration.UpBow,
            'v' => Decoration.DownBow,
            _ => (Decoration?)null
        };

        if (result == null)
        {
            return false;
        }

        decoration = result.Value;
        index++; // Consume character
        return true;
    }
}
