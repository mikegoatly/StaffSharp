using StaffSharp.Notation;

namespace StaffSharp.Performance;

/// <summary>
/// Represents a time signature change at a specific point in musical time.
/// </summary>
/// <param name="TimeInBeats">The musical time (in beats from start) when this time signature begins.</param>
/// <param name="TimeSignature">The new time signature.</param>
public sealed record TimeSignatureChange(
    Rational TimeInBeats,
    TimeSignature TimeSignature);
