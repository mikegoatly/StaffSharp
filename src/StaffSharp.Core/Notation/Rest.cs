namespace StaffSharp.Core.Notation;

/// <summary>
/// Represents a rest (silence) in notation.
/// </summary>
public record Rest(SymbolicDuration Duration) : INotationEvent
{
    public static readonly Rest Whole = new(SymbolicDuration.Whole);
    public static readonly Rest Half = new(SymbolicDuration.Half);
    public static readonly Rest Quarter = new(SymbolicDuration.Quarter);
    public static readonly Rest Eighth = new(SymbolicDuration.Eighth);
}
