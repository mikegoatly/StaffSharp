namespace StaffSharp.Audio.Analysis.Pitch;

/// <summary>
/// Represents a single pitch candidate detected by a pitch detection algorithm.
/// </summary>
/// <param name="FrequencyHz">The frequency of this candidate in Hz.</param>
/// <param name="Probability">The probability of this candidate being the true pitch [0.0, 1.0].</param>
public readonly record struct PitchCandidate(double FrequencyHz, float Probability);
