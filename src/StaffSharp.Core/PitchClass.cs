namespace StaffSharp;

/// <summary>
/// Represents the 12 pitch classes in Western music, each assigned an integer 
/// value from 0 to 11 corresponding to semitones.
/// </summary>
public enum PitchClass
{
    C = 0,
    CSharp = 1,
    D = 2,
    DSharp = 3,
    E = 4,
    F = 5,
    FSharp = 6,
    G = 7,
    GSharp = 8,
    A = 9,
    ASharp = 10,
    B = 11
}

public static class PitchClassExtensions
{
    /// <summary>
    /// Gets the name of the pitch class as a string.
    /// </summary>
    public static string GetName(this PitchClass pitchClass) => pitchClass switch
    {
        PitchClass.C => "C",
        PitchClass.CSharp => "C#",
        PitchClass.D => "D",
        PitchClass.DSharp => "Eb",
        PitchClass.E => "E",
        PitchClass.F => "F",
        PitchClass.FSharp => "F#",
        PitchClass.G => "G",
        PitchClass.GSharp => "Ab",
        PitchClass.A => "A",
        PitchClass.ASharp => "Bb",
        PitchClass.B => "B",
        _ => "?"
    };
}