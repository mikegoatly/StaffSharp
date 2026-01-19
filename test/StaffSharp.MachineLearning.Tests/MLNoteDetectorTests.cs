namespace StaffSharp.MachineLearning.Tests;

using StaffSharp.Audio;
using StaffSharp.Audio.Pipeline;
using StaffSharp.MachineLearning.Options;
using StaffSharp.TestHelpers.Builders;

public sealed class MLNoteDetectorTests
{
    [Fact]
    public async Task DetectAsync_WithLeadingSilence_TrimsFromNoteOnsets()
    {
        // Arrange
        using var detector = MLNoteDetector.Create();

        const int sampleRate = 16000;
        const double leadingSilenceDuration = 2.0; // 2 seconds of silence
        const double noteDuration = 0.25; // quarter note
        const double totalDuration = leadingSilenceDuration + (noteDuration * 8);

        // Build audio: [2s silence] + [8 quarter notes at 120 BPM]
        var builder = AudioSignalBuilder.Create()
            .WithSampleRate(sampleRate)
            .WithDuration(totalDuration);

        // Add 8 notes starting at 2 seconds
        for (int i = 0; i < 8; i++)
        {
            builder.AtTime(leadingSilenceDuration + (i * noteDuration))
                   .AddSine(440.0, noteDuration * 0.9); // Slight gap between notes
        }

        var samples = builder.Build();
        var audio = new AudioBuffer(samples, sampleRate, channels: 1);

        // Act
        var result = await detector.DetectAsync(PipelineProgress.Null, audio);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Events);

        // The first note should start near time 0 (not at 2 seconds)
        // Allow small tolerance for boundary detection precision
        var firstEvent = result.Events[0];
        var firstNoteOnsetSeconds = result.TempoMap.BeatsToSeconds(firstEvent.OnsetBeats);
        Assert.True(firstNoteOnsetSeconds < 0.5,
            $"Expected first note onset near 0s after trimming, but got {firstNoteOnsetSeconds:F3}s");
    }

    [Fact]
    public async Task DetectAsync_WithoutLeadingSilence_PreservesOnsets()
    {
        // Arrange
        using var detector = MLNoteDetector.Create();

        const int sampleRate = 16000;
        const double noteDuration = 0.25; // quarter note
        const double totalDuration = noteDuration * 8;

        // Build audio: immediate note start (no silence), 8 quarter notes at 120 BPM
        var builder = AudioSignalBuilder.Create()
            .WithSampleRate(sampleRate)
            .WithDuration(totalDuration);

        // Add 8 notes starting immediately
        for (int i = 0; i < 8; i++)
        {
            builder.AtTime(i * noteDuration)
                   .AddSine(440.0, noteDuration * 0.9); // Slight gap between notes
        }

        var samples = builder.Build();
        var audio = new AudioBuffer(samples, sampleRate, channels: 1);

        // Act
        var result = await detector.DetectAsync(PipelineProgress.Null, audio);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Events);

        // First note should start at or very near time 0
        // Note: Some delay is expected due to onset detection and quantization
        var firstEvent = result.Events[0];
        var firstNoteOnsetSeconds = result.TempoMap.BeatsToSeconds(firstEvent.OnsetBeats);
        Assert.True(firstNoteOnsetSeconds < 0.6,
            $"Expected first note onset near 0s (with ML onset detection delay), but got {firstNoteOnsetSeconds:F3}s");
    }

    [Fact]
    public async Task DetectAsync_WithNullAudio_ThrowsArgumentNullException()
    {
        // Arrange
        using var detector = MLNoteDetector.Create();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => detector.DetectAsync(PipelineProgress.Null, null!));
    }
}
