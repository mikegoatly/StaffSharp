namespace StaffSharp.Notation;

/// <summary>
/// Represents a lyric line associated with notes in a measure.
/// ABC notation: w: Do re mi fa | sol la ti do |
/// </summary>
/// <param name="Syllables">
/// The syllables in this lyric line.
/// Each syllable aligns with a note or chord in the measure.
/// </param>
public readonly record struct Lyric(IReadOnlyList<LyricSyllable> Syllables);

/// <summary>
/// Represents a single syllable in a lyric line.
/// </summary>
/// <param name="Text"> The syllable text. </param>
/// <param name="Type"> The type of syllable (affects hyphenation/spacing). </param>
public readonly record struct LyricSyllable(string Text, LyricSyllableType Type = LyricSyllableType.Standalone);

/// <summary>
/// Type of lyric syllable.
/// </summary>
public enum LyricSyllableType
{
    /// <summary>
    /// Standalone word (no hyphen).
    /// ABC: "Do"
    /// </summary>
    Standalone,

    /// <summary>
    /// Start of word (hyphen follows).
    /// ABC: "Twin-"
    /// </summary>
    Start,

    /// <summary>
    /// Middle of word (hyphens on both sides).
    /// ABC: "-kle-"
    /// </summary>
    Middle,

    /// <summary>
    /// End of word (hyphen precedes).
    /// ABC: "-star"
    /// </summary>
    End,

    /// <summary>
    /// Hold syllable for multiple notes.
    /// ABC: * or _
    /// </summary>
    Hold,

    /// <summary>
    /// Blank (skip this note).
    /// ABC: ~ or (space)
    /// </summary>
    Blank
}
