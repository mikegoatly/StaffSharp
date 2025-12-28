namespace StaffSharp.Core.Notation;

/// <summary>
/// Represents a part (instrument/staff) in a score.
/// A part can contain multiple voices that share the same clef.
/// </summary>
public class Part
{
    public Part(string name, Clef clef, IReadOnlyList<Voice> voices)
    {
        Name = name;
        Clef = clef;
        Voices = voices;
    }

    public string Name { get; }
    public Clef Clef { get; }
    public IReadOnlyList<Voice> Voices { get; }
}
