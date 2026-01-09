namespace StaffSharp.Notation;

/// <summary>
/// Represents a part (instrument) in a score.
/// A part can contain one or more staves (single staff for most instruments, grand staff for piano).
/// </summary>
public class Part
{
    /// <summary>
    /// Creates a new part with multiple staves (e.g., grand staff for piano).
    /// </summary>
    public Part(string name, IReadOnlyList<Staff> staves)
    {
        if (staves == null || staves.Count == 0)
        {
            throw new ArgumentException("Part must have at least one staff", nameof(staves));
        }

        Name = name;
        Staves = staves;
    }

    /// <summary>
    /// Creates a new part with a single staff (legacy constructor for backward compatibility).
    /// </summary>
    public Part(string name, Clef clef, IReadOnlyList<Voice> voices)
        : this(name, [new Staff(1, clef, voices)])
    {
    }

    public string Name { get; }

    /// <summary>
    /// Staves in this part. Single-staff parts have one staff, grand staff (piano) has two.
    /// </summary>
    public IReadOnlyList<Staff> Staves { get; }

    /// <summary>
    /// Gets whether this part uses a grand staff (multiple staves).
    /// </summary>
    public bool IsGrandStaff => Staves.Count > 1;

    /// <summary>
    /// Clef of the first staff (for backward compatibility with single-staff code).
    /// </summary>
    public Clef Clef => Staves[0].Clef;

    /// <summary>
    /// All voices from all staves (for backward compatibility with single-staff code).
    /// </summary>
    public IReadOnlyList<Voice> Voices => Staves.SelectMany(s => s.Voices).ToList();

    // Part-level slur spans (cross-measure/system/grand-staff capable)
    public IList<SlurSpan> Slurs {get;} = [];
}
