namespace StaffSharp.Abc.Exporting;

using System.Globalization;
using System.Text;

using StaffSharp.Notation;

/// <summary>
/// Formats barlines as ABC notation.
/// </summary>
internal static class AbcBarlineFormatter
{
    /// <summary>
    /// Formats a barline as ABC notation.
    /// </summary>
    /// <param name="endBarline">The barline type at the end of the current measure.</param>
    /// <param name="nextStartBarline">The barline type at the start of the next measure (for repeats).</param>
    /// <param name="repeatVariants">Optional repeat variant numbers (e.g., [1, [2).</param>
    /// <returns>
    /// The ABC barline notation:
    /// - | (normal)
    /// - || (double bar)
    /// - |] (final)
    /// - |: (repeat start)
    /// - :| (repeat end)
    /// - :: (repeat both)
    /// - [1, [2, etc. (repeat variants)
    /// </returns>
    /// <remarks>
    /// Reverses the logic in AbcParser.ParseBarlineTypes (line 372-424).
    /// </remarks>
    public static string Format(
        BarlineType? endBarline,
        BarlineType? nextStartBarline,
        IReadOnlyList<int>? repeatVariants = null)
    {
        var result = new StringBuilder();

        // Repeat variant prefix (e.g., [1, [2)
        if (repeatVariants != null && repeatVariants.Count > 0)
        {
            result.Append('[');
            for (int i = 0; i < repeatVariants.Count; i++)
            {
                if (i > 0)
                {
                    result.Append(',');
                }
                result.Append(repeatVariants[i].ToString(CultureInfo.InvariantCulture));
            }
            // Note: No space after variant numbers in ABC notation
        }

        // Determine barline symbol based on end and next start types
        if (endBarline == BarlineType.RepeatEnd && nextStartBarline == BarlineType.RepeatStart)
        {
            // Repeat both (:|:) - but ABC standard uses "::" for this
            result.Append("::");
        }
        else if (endBarline == BarlineType.RepeatEnd)
        {
            // Repeat end only
            result.Append(":|");
        }
        else if (nextStartBarline == BarlineType.RepeatStart)
        {
            // Normal end, repeat start next
            result.Append("|:");
        }
        else if (endBarline == BarlineType.Final)
        {
            // Final barline
            result.Append("|]");
        }
        else if (endBarline == BarlineType.DoubleBar)
        {
            // Double barline
            result.Append("||");
        }
        else
        {
            // Normal barline or null (default to normal)
            result.Append('|');
        }

        return result.ToString();
    }
}
