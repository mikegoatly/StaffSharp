namespace StaffSharp.Notation;

/// <summary>
/// Represents a voice (sequence of measures).
/// </summary>
public class Voice
{
    public Voice(int number, IReadOnlyList<Measure> measures)
    {
        Number = number;
        Measures = measures;
    }

    public int Number { get; }
    public IReadOnlyList<Measure> Measures { get; }
}
