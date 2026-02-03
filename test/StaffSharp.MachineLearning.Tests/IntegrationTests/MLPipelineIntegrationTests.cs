using StaffSharp.Audio.Pipeline;
using StaffSharp.Notation;
using StaffSharp.TestHelpers;

using Xunit.Abstractions;

namespace StaffSharp.MachineLearning.Tests.IntegrationTests;

public class MLPipelineIntegrationTests(ITestOutputHelper outputHelper)
{
    [InlineData(@"Scales\c-scale-44100-mono.wav")]
    [InlineData(@"Scales\c-scale-44100-stereo.wav")]
    [Theory]
    public async Task ParsingCScale_ResultsInExpectedScore(string testFile)
    {
        var score = await ExtractScoreFromAudioUsingML(testFile);

        score.AssertSequence(0)
            .Note(PitchClass.C, SymbolicDuration.Quarter, octave: 4)
            .Note(PitchClass.D, SymbolicDuration.Quarter, octave: 4)
            .Note(PitchClass.E, SymbolicDuration.Quarter, octave: 4)
            .Note(PitchClass.F, SymbolicDuration.Quarter, octave: 4)
            .AndNoMore();

        score.AssertSequence(1)
            .Note(PitchClass.G, SymbolicDuration.Quarter, octave: 4)
            .Note(PitchClass.A, SymbolicDuration.Quarter, octave: 4)
            .Note(PitchClass.B, SymbolicDuration.Quarter, octave: 4)
            .Note(PitchClass.C, SymbolicDuration.Quarter, octave: 5)
            .AndNoMore();
    }

    [Fact]
    public async Task ParsingDScale_ResultsInExpectedScore()
    {
        var score = await ExtractScoreFromAudioUsingML(@"Scales\d-scale.wav");

        score.AssertSequence(0)
            .Note(PitchClass.D, SymbolicDuration.Quarter, octave: 4)
            .Note(PitchClass.E, SymbolicDuration.Quarter, octave: 4)
            .Note(PitchClass.FSharp, SymbolicDuration.Quarter, octave: 4)
            .Note(PitchClass.G, SymbolicDuration.Quarter, octave: 4)
            .AndNoMore();

        score.AssertSequence(1)
            .Note(PitchClass.A, SymbolicDuration.Quarter, octave: 4)
            .Note(PitchClass.B, SymbolicDuration.Quarter, octave: 4)
            .Note(PitchClass.CSharp, SymbolicDuration.Quarter, octave: 5)
            .Note(PitchClass.D, SymbolicDuration.Quarter, octave: 5)
            .AndNoMore();
    }

    [Fact]
    public async Task ParsingEScaleReverse_ResultsInExpectedScore()
    {
        var score = await ExtractScoreFromAudioUsingML(@"Scales\e-scale-reverse.wav");

        score.AssertSequence(0)
            .Note(PitchClass.E, SymbolicDuration.Quarter, octave: 5)
            .Note(PitchClass.DSharp, SymbolicDuration.Quarter, octave: 5)
            .Note(PitchClass.CSharp, SymbolicDuration.Quarter, octave: 5)
            .Note(PitchClass.B, SymbolicDuration.Quarter, octave: 4)
            .AndNoMore();

        score.AssertSequence(1)
            .Note(PitchClass.A, SymbolicDuration.Quarter, octave: 4)
            .Note(PitchClass.GSharp, SymbolicDuration.Quarter, octave: 4)
            .Note(PitchClass.FSharp, SymbolicDuration.Quarter, octave: 4)
            .Note(PitchClass.E, SymbolicDuration.Quarter, octave: 4)
            .AndNoMore();
    }

    private async Task<NotationScore> ExtractScoreFromAudioUsingML(string testFile, AudioPipelineOptions? options = null)
    {
        using var inputStream = File.OpenRead(@$"TestData\{testFile}");

        options = (options ?? new AudioPipelineOptions()) with
        {
            NoteDetector = new MLNoteDetector(),
            DiagnosticsCollector = new XUnitDiagnosticsCollector(outputHelper)
        };

        return await AudioPipeline.FromWavAsync(inputStream, options);
    }
}
