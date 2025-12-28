namespace StaffSharp.Core.Notation;

/// <summary>
/// Base interface for all notation events (notes, rests).
/// </summary>
public interface INotationEvent
{
    SymbolicDuration Duration { get; }
}
