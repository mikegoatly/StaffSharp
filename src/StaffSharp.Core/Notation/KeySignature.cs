namespace StaffSharp.Core.Notation;

/// <summary>
/// Represents a key signature (number of sharps or flats).
/// </summary>
public record KeySignature(int Sharps)
{
    /// <summary>
    /// Creates a key signature with the specified number of sharps (positive) or flats (negative).
    /// </summary>
    /// <param name="sharps">Number of sharps (positive) or flats (negative). Range: -7 to 7.</param>
    public static KeySignature Create(int sharps)
    {
        if (sharps < -7 || sharps > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(sharps), "Key signature must be between -7 (flats) and 7 (sharps).");
        }
        return new KeySignature(sharps);
    }

    // Common major keys
    public static readonly KeySignature C = new(0);      // No sharps/flats
    public static readonly KeySignature G = new(1);      // 1 sharp
    public static readonly KeySignature D = new(2);      // 2 sharps
    public static readonly KeySignature A = new(3);      // 3 sharps
    public static readonly KeySignature E = new(4);      // 4 sharps
    public static readonly KeySignature B = new(5);      // 5 sharps
    public static readonly KeySignature FSharp = new(6); // 6 sharps
    public static readonly KeySignature CSharp = new(7); // 7 sharps

    public static readonly KeySignature F = new(-1);     // 1 flat
    public static readonly KeySignature BFlat = new(-2); // 2 flats
    public static readonly KeySignature EFlat = new(-3); // 3 flats
    public static readonly KeySignature AFlat = new(-4); // 4 flats
    public static readonly KeySignature DFlat = new(-5); // 5 flats
    public static readonly KeySignature GFlat = new(-6); // 6 flats
    public static readonly KeySignature CFlat = new(-7); // 7 flats

    /// <summary>
    /// Gets whether this key has sharps (true), flats (false), or neither (C major).
    /// </summary>
    public bool HasSharps => Sharps > 0;

    /// <summary>
    /// Gets whether this key has flats.
    /// </summary>
    public bool HasFlats => Sharps < 0;

    /// <summary>
    /// Gets the number of flats (positive number when key has flats, 0 otherwise).
    /// </summary>
    public int FlatCount => Sharps < 0 ? -Sharps : 0;

    /// <summary>
    /// Gets the number of sharps (positive number when key has sharps, 0 otherwise).
    /// </summary>
    public int SharpCount => Sharps > 0 ? Sharps : 0;
}
