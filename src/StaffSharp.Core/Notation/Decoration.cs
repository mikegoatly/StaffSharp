namespace StaffSharp.Notation;

/// <summary>
/// Musical decorations and articulations.
/// Corresponds to ABC notation decorations (!symbol! or shorthand).
/// </summary>
public enum Decoration
{
    // Articulations
    Staccato,           // . (dot)
    Accent,             // L or >
    Tenuto,             // -
    Marcato,            // ^

    // Ornaments
    Trill,              // !trill! or T
    Mordent,            // !mordent! or M
    LowerMordent,       // !lowermordent!
    UpperMordent,       // !uppermordent!
    Turn,               // !turn!
    InvertedTurn,       // !invertedturn!
    Roll,               // ~ (Irish roll)

    // Holds
    Fermata,            // !fermata! or H
    Breath,             // !breath!

    // Bowing (strings)
    UpBow,              // u or !upbow!
    DownBow,            // v or !downbow!

    // Dynamics
    Pianissimo,         // !pp!
    Piano,              // !p!
    MezzoPiano,         // !mp!
    MezzoForte,         // !mf!
    Forte,              // !f!
    Fortissimo,         // !ff!
    Sforzando,          // !sfz!
    Crescendo,          // !crescendo! or !<(!
    Diminuendo,         // !diminuendo! or !>(!

    // Pedal (piano)
    Pedal,              // !pedal!
    PedalUp             // !pedal-up!
}
