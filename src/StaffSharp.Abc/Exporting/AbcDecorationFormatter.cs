namespace StaffSharp.Abc.Exporting;

using StaffSharp.Notation;

/// <summary>
/// Formats decorations to ABC notation.
/// </summary>
internal static class AbcDecorationFormatter
{
    /// <summary>
    /// Formats a decoration to ABC notation.
    /// Prefers shorthand notation when available for brevity.
    /// </summary>
    public static string Format(Decoration decoration)
    {
        return decoration switch
        {
            // Shorthand decorations (preferred for brevity)
            Decoration.Staccato => ".",
            Decoration.Roll => "~",
            Decoration.Trill => "T",
            Decoration.Mordent => "M",
            Decoration.Fermata => "H",
            Decoration.Accent => "L",
            Decoration.UpBow => "u",
            Decoration.DownBow => "v",

            // Named decorations (no shorthand available)
            Decoration.Tenuto => "!tenuto!",
            Decoration.Marcato => "!marcato!",
            Decoration.LowerMordent => "!lowermordent!",
            Decoration.UpperMordent => "!uppermordent!",
            Decoration.Turn => "!turn!",
            Decoration.InvertedTurn => "!invertedturn!",
            Decoration.Breath => "!breath!",

            // Dynamics
            Decoration.Pianissimo => "!pp!",
            Decoration.Piano => "!p!",
            Decoration.MezzoPiano => "!mp!",
            Decoration.MezzoForte => "!mf!",
            Decoration.Forte => "!f!",
            Decoration.Fortissimo => "!ff!",
            Decoration.Sforzando => "!sfz!",
            Decoration.Crescendo => "!crescendo!",
            Decoration.Diminuendo => "!diminuendo!",

            // Pedal
            Decoration.Pedal => "!pedal!",
            Decoration.PedalUp => "!pedal-up!",

            _ => string.Empty
        };
    }
}
