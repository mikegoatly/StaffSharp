namespace StaffSharp.Notation;

/// <summary>
/// Represents a chord symbol (harmony notation) in a score.
/// ABC notation: "Cmaj7"C4 or "Dm7"D2
/// </summary>
public record ChordSymbol
{
    public ChordSymbol(string symbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol, nameof(symbol));
        Symbol = symbol;
    }

    /// <summary>
    /// The chord symbol text (e.g., "Cmaj7", "Dm", "G7", "F#m7b5").
    /// </summary>
    public string Symbol { get; }
}
