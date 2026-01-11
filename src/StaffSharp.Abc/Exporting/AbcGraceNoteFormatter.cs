namespace StaffSharp.Abc.Exporting;

using System.Text;

using StaffSharp.Notation;

/// <summary>
/// Formats grace notes to ABC notation.
/// </summary>
internal static class AbcGraceNoteFormatter
{
    /// <summary>
    /// Formats a grace note to ABC notation.
    /// Format: {ABC} for appoggiatura, {/ABC} for acciaccatura.
    /// </summary>
    public static string Format(GraceNote graceNote)
    {
        var sb = new StringBuilder();
        sb.Append('{');

        if (graceNote.IsAcciaccatura)
        {
            sb.Append('/');
        }

        foreach (var pitch in graceNote.Pitches)
        {
            sb.Append(AbcPitchFormatter.Format(pitch));
        }

        sb.Append('}');
        return sb.ToString();
    }
}
