namespace StaffSharp.Core;

/// <summary>
/// Represents a musical tempo in beats per minute (BPM).
/// </summary>
public readonly record struct Tempo : IComparable<Tempo>, IComparable
{
    private Tempo(float bpm)
    {
        Bpm = bpm;
    }

    /// <summary>
    /// Gets the tempo in beats per minute.
    /// </summary>
    public float Bpm { get; }

    /// <summary>
    /// Creates a tempo with validation.
    /// </summary>
    /// <param name="bpm">Beats per minute (must be positive).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when BPM is not positive.</exception>
    public static Tempo Create(float bpm)
    {
        if (bpm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bpm), "Tempo must be positive.");
        }

        return new Tempo(bpm);
    }

    /// <summary>
    /// Gets the duration of a single beat at this tempo.
    /// </summary>
    public TimeSpan BeatDuration => TimeSpan.FromMinutes(1.0 / Bpm);

    /// <summary>
    /// Converts a time duration to beats at this tempo.
    /// </summary>
    public double TimeToBeats(TimeSpan duration) => duration.TotalMinutes * Bpm;

    /// <summary>
    /// Converts beats to time duration at this tempo.
    /// </summary>
    public TimeSpan BeatsToTime(double beats) => TimeSpan.FromMinutes(beats / Bpm);

    /// <summary>
    /// Common tempo markings.
    /// </summary>
    public static readonly Tempo Largo = Create(50);
    public static readonly Tempo Adagio = Create(70);
    public static readonly Tempo Andante = Create(90);
    public static readonly Tempo Moderato = Create(110);
    public static readonly Tempo Allegro = Create(140);
    public static readonly Tempo Presto = Create(180);

    public override string ToString() => $"{Bpm:F1} BPM";

    // Comparison operators
    public static bool operator >(Tempo a, Tempo b) => a.Bpm > b.Bpm;
    public static bool operator <(Tempo a, Tempo b) => a.Bpm < b.Bpm;
    public static bool operator >=(Tempo a, Tempo b) => a.Bpm >= b.Bpm;
    public static bool operator <=(Tempo a, Tempo b) => a.Bpm <= b.Bpm;

    public int CompareTo(Tempo other) => Bpm.CompareTo(other.Bpm);

    public int CompareTo(object? obj)
    {
        if (obj is null)
        {
            return 1;
        }

        if (obj is Tempo other)
        {
            return CompareTo(other);
        }

        throw new ArgumentException($"Object must be of type {nameof(Tempo)}");
    }
}
