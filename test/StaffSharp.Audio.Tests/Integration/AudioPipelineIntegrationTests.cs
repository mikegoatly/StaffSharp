using StaffSharp.Audio.Analysis.Boundaries;
using StaffSharp.Audio.Analysis.Onset;
using StaffSharp.Audio.Analysis.Pitch;
using StaffSharp.Audio.Pipeline;
using StaffSharp.Audio.Pipeline.Stages;
using StaffSharp.TestHelpers.Builders;

namespace StaffSharp.Audio.Tests.Integration;

/// <summary>
/// Integration tests for the audio pipeline.
/// These tests verify that multiple stages work together correctly with real detectors,
/// focusing on data flow, progress reporting, and diagnostics across the pipeline.
/// </summary>
public sealed class AudioPipelineIntegrationTests
{
    private const int SampleRate = 44100;

    [Fact]
    public async Task ExecuteAsync_ThreeStages_LoadBoundariesOnsets_DetectsCorrectly()
    {
        // Arrange: 3 clear notes with attacks - realistic end-to-end scenario
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(1.8)
            .AtTime(0.0).WithAttack(0.02).AddSine(261.63, amplitude: 0.5, durationSeconds: 0.5) // C4
            .AtTime(0.6).WithAttack(0.02).AddSine(329.63, amplitude: 0.5, durationSeconds: 0.5) // E4
            .AtTime(1.2).WithAttack(0.02).AddSine(392.00, amplitude: 0.5, durationSeconds: 0.5) // G4
            .Build();

        var audio = new AudioBuffer(samples, SampleRate, 1);
        using var stream = WavStreamBuilder.FromAudioBuffer(audio);
        var context = new AudioPipelineContext();

        var onsetDetectorOptions = new OnsetDetectionOptions
        {
            Threshold = 0.3f, // Higher threshold to avoid false positives
            MinOnsetIntervalSeconds = 0.1f
        };

        var pipeline = AsyncAudioPipeline.Create(stream)
            .AddStage(new LoadAudioStage())
            .AddStage(new DetectBoundariesStage(new EnergyBasedBoundaryDetector()))
            .AddStage(new DetectOnsetsStage(new SpectralFluxOnsetDetector(onsetDetectorOptions)));

        // Act
        var onsets = await pipeline.ExecuteAsync(context);

        // Assert - verify data flows through all stages correctly
        Assert.NotNull(context.Audio);
        Assert.NotNull(context.Boundaries);
        Assert.NotNull(context.Onsets);
        Assert.NotNull(onsets);
        Assert.True(onsets.Length > 0, "Should detect at least one onset");
        Assert.True(onsets.Length < 50, "Should not detect an excessive number of onsets");
    }

    [Fact]
    public async Task ExecuteAsync_FourStages_LoadBoundariesOnsetsPitches_DetectsCorrectly()
    {
        // Arrange: Multiple notes with different pitches - tests pitch detection in pipeline
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(1.5)
            .AtTime(0.0).WithAttack(0.02).AddHarmonics(261.63, harmonicCount: 5, amplitude: 0.5, durationSeconds: 0.4) // C4
            .AtTime(0.5).WithAttack(0.02).AddHarmonics(329.63, harmonicCount: 5, amplitude: 0.5, durationSeconds: 0.4) // E4
            .AtTime(1.0).WithAttack(0.02).AddHarmonics(392.00, harmonicCount: 5, amplitude: 0.5, durationSeconds: 0.4) // G4
            .Build();

        var audio = new AudioBuffer(samples, SampleRate, 1);
        using var stream = WavStreamBuilder.FromAudioBuffer(audio);
        var context = new AudioPipelineContext();

        var pipeline = AsyncAudioPipeline.Create(stream)
            .AddStage(new LoadAudioStage())
            .AddStage(new DetectBoundariesStage(new EnergyBasedBoundaryDetector()))
            .AddStage(new DetectOnsetsStage(new SpectralFluxOnsetDetector()))
            .AddStage(new DetectPitchesStage(new YinPitchDetector()));

        // Act
        var pitches = await pipeline.ExecuteAsync(context);

        // Assert - verify all stages executed and populated context
        Assert.NotNull(context.Audio);
        Assert.NotNull(context.Boundaries);
        Assert.NotNull(context.Onsets);
        Assert.NotNull(context.Pitches);
        Assert.NotNull(pitches);
        Assert.True(pitches.Length > 0);

        // Verify at least some pitches were detected (not all unpitched)
        Assert.Contains(pitches, p => p != DetectPitchesStage.UnpitchedSentinel);
    }

    [Fact]
    public async Task ExecuteAsync_WithProgressReporting_ReportsAllStages()
    {
        // Arrange: Multi-stage pipeline to test progress reporting
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(1.0)
            .AtTime(0.1).WithAttack(0.02).AddSine(440.0, amplitude: 0.5, durationSeconds: 0.5)
            .Build();

        var audio = new AudioBuffer(samples, SampleRate, 1);
        using var stream = WavStreamBuilder.FromAudioBuffer(audio);

        var progressReports = new List<PipelineProgress>();
        var progress = new Progress<PipelineProgress>(p => progressReports.Add(p));
        var context = new AudioPipelineContext(progress: progress);

        var pipeline = AsyncAudioPipeline.Create(stream)
            .AddStage(new LoadAudioStage())
            .AddStage(new DetectBoundariesStage(new EnergyBasedBoundaryDetector()))
            .AddStage(new DetectOnsetsStage(new SpectralFluxOnsetDetector()));

        // Act
        await pipeline.ExecuteAsync(context);

        // Assert - verify progress was reported for each stage
        Assert.NotEmpty(progressReports);
        Assert.Contains(progressReports, p => p.StageName == "LoadAudio");
        Assert.Contains(progressReports, p => p.StageName == "DetectBoundaries");
        Assert.Contains(progressReports, p => p.StageName == "DetectOnsets");

        // Verify all three unique stages were reported
        var uniqueStages = progressReports.Select(p => p.StageName).Distinct().ToList();
        Assert.Equal(3, uniqueStages.Count);
    }

    [Fact]
    public async Task ExecuteAsync_WithDiagnostics_CollectsFromAllStages()
    {
        // Arrange: Multi-stage pipeline to test diagnostics collection
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(1.0)
            .AtTime(0.1).WithAttack(0.02).AddSine(440.0, amplitude: 0.5, durationSeconds: 0.5)
            .Build();

        var audio = new AudioBuffer(samples, SampleRate, 1);
        using var stream = WavStreamBuilder.FromAudioBuffer(audio);

        var diagnostics = new MemoryDiagnosticsCollector();
        var context = new AudioPipelineContext(diagnosticsCollector: diagnostics);

        var pipeline = AsyncAudioPipeline.Create(stream)
            .AddStage(new LoadAudioStage())
            .AddStage(new DetectBoundariesStage(new EnergyBasedBoundaryDetector()))
            .AddStage(new DetectOnsetsStage(new SpectralFluxOnsetDetector()));

        // Act
        await pipeline.ExecuteAsync(context);

        // Assert - verify diagnostics collected from all stages
        var entries = diagnostics.GetEntries().ToList();

        // LoadAudio stage diagnostics
        Assert.Contains(entries, e => e.StageName == "LoadAudio" && e.Key == "SampleRate");
        Assert.Contains(entries, e => e.StageName == "LoadAudio" && e.Key == "Channels");

        // DetectBoundaries stage diagnostics
        Assert.Contains(entries, e => e.StageName == "DetectBoundaries" && e.Key == "StartSample");
        Assert.Contains(entries, e => e.StageName == "DetectBoundaries" && e.Key == "EndSample");

        // DetectOnsets stage diagnostics
        Assert.Contains(entries, e => e.StageName == "DetectOnsets" && e.Key == "OnsetCount");

        // Verify diagnostics from all 3 stages are present
        var stageNames = entries.Select(e => e.StageName).Distinct().ToList();
        Assert.Contains("LoadAudio", stageNames);
        Assert.Contains("DetectBoundaries", stageNames);
        Assert.Contains("DetectOnsets", stageNames);
    }

    [Fact]
    public async Task ExecuteAsync_WithSilencePadding_HandlesCorrectly()
    {
        // Arrange: Audio with leading/trailing silence - tests boundary detection integration
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(2.5)
            .AtTime(1.0) // 1 second leading silence
            .WithAttack(0.05)
            .AddSine(440.0, amplitude: 0.5, durationSeconds: 0.5)
            // 1 second trailing silence
            .Build();

        var audio = new AudioBuffer(samples, SampleRate, 1);
        using var stream = WavStreamBuilder.FromAudioBuffer(audio);
        var context = new AudioPipelineContext();

        var pipeline = AsyncAudioPipeline.Create(stream)
            .AddStage(new LoadAudioStage())
            .AddStage(new DetectBoundariesStage(new EnergyBasedBoundaryDetector()))
            .AddStage(new DetectOnsetsStage(new SpectralFluxOnsetDetector()));

        // Act
        var onsets = await pipeline.ExecuteAsync(context);

        // Assert - verify boundaries were detected and used
        Assert.NotNull(context.Boundaries);
        Assert.InRange(context.Boundaries.LeadingSilence.TotalSeconds, 0.8, 1.2); // ~1 second
        Assert.InRange(context.Boundaries.TrailingSilence.TotalSeconds, 0.8, 1.2); // ~1 second

        // Verify onsets were detected despite silence padding
        Assert.NotNull(onsets);
        Assert.True(onsets.Length > 0, "Should detect at least one onset despite silence padding");
    }
}
