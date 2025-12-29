namespace StaffSharp.Notation;

/// <summary>
/// Base duration values for musical notes (whole, half, quarter, etc.).
/// </summary>
#pragma warning disable CA1027 // Mark enums with FlagsAttribute - values are intentional and aren't flags
public enum NoteDurationBase
#pragma warning restore CA1027 // Mark enums with FlagsAttribute
{
    Unspecified = 0,
    Whole = 1,
    Half = 2,
    Quarter = 4,
    Eighth = 8,
    Sixteenth = 16,
    ThirtySecond = 32
}
