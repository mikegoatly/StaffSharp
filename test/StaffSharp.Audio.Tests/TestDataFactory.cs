using StaffSharp;
using StaffSharp.Audio;
using StaffSharp.Audio.Analysis.Boundaries;
using StaffSharp.Performance;

namespace StaffSharp.Audio.Tests;

/// <summary>
/// Factory methods for creating test data with correct constructors.
/// </summary>
internal static class TestDataFactory
{
    /// <summary>
    /// Creates an AudioBoundaries instance for testing.
    /// </summary>
    public static AudioBoundaries CreateAudioBoundaries(
        AudioBuffer audio,
        int startSample,
        int endSample,
        TimeSpan? leadingSilence = null,
        TimeSpan? trailingSilence = null)
    {
        return new AudioBoundaries(
            startSample,
            endSample,
            audio.SampleRate,
            leadingSilence ?? TimeSpan.Zero,
            trailingSilence ?? TimeSpan.Zero);
    }
}
