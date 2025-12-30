namespace StaffSharp.Importers.Abc;

using StaffSharp.Notation;

/// <summary>
/// Parses ABC inline headers like [K:G], [M:3/4], etc.
/// Inline headers can appear within the music body and affect subsequent events.
/// </summary>
internal static class AbcInlineHeaderParser
{
    /// <summary>
    /// Checks if the current position contains an inline header.
    /// Pattern: [Letter:value]
    /// </summary>
    public static bool IsInlineHeader(string input, int index)
    {
        if (index >= input.Length || input[index] != '[')
        {
            return false;
        }

        // Need at least [X:y]
        if (index + 4 >= input.Length)
        {
            return false;
        }

        // Check for pattern: [ + Letter + :
        return char.IsLetter(input[index + 1]) && input[index + 2] == ':';
    }

    /// <summary>
    /// Tries to parse an inline header and update the parsing state.
    /// Returns true if successfully parsed, false otherwise.
    /// </summary>
    public static bool TryParseInlineHeader(
        string input,
        ref int index,
        ref KeySignature keySignature,
        ref TimeSignature? timeSignature)
    {
        if (!IsInlineHeader(input, index))
        {
            return false;
        }

        var startIndex = index;
        index++; // Skip [

        char headerType = input[index];
        index++; // Skip header letter

        if (index >= input.Length || input[index] != ':')
        {
            index = startIndex;
            return false;
        }

        index++; // Skip :

        // Find closing bracket
        int closeIndex = input.IndexOf(']', index);
        if (closeIndex == -1)
        {
            index = startIndex;
            return false;
        }

        var headerValue = input[index..closeIndex].Trim();
        index = closeIndex + 1; // Move past ]

        // Parse based on header type
        switch (char.ToUpperInvariant(headerType))
        {
            case 'K':
                keySignature = AbcHeaderParser.ParseKeySignature(headerValue);
                return true;

            case 'M':
                timeSignature = AbcHeaderParser.ParseTimeSignature(headerValue);
                return true;

            case 'L':
                // Default note length changes - we'd need to pass this through
                // For now, skip it
                return true;

            case 'Q':
                // Tempo changes - we'd need to pass this through
                // For now, skip it
                return true;

            default:
                // Unknown header type, but we successfully parsed it
                return true;
        }
    }
}
