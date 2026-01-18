namespace StaffSharp.Synthesis.Internal;

/// <summary>
/// Represents a synthesizable note with pitch, velocity, and absolute timing.
/// </summary>
internal sealed record SynthNote(MidiNote Pitch, float Velocity, double OnsetSeconds, double OffsetSeconds);
