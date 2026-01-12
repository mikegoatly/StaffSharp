using StaffSharp.Audio.Pipeline;
using StaffSharp.Notation;
using StaffSharp.TestHelpers;

using Xunit.Abstractions;

namespace StaffSharp.Audio.Tests.IntegrationTests
{
    public class AudioPipelineIntegrationTests(ITestOutputHelper outputHelper)
    {
        [Fact]
        public async Task ParsingCScale_ResultsInExpectedScore()
        {
            var score = await ExtractScoreFromAudio(@"Scales\c-scale-44100-mono.wav");

            // We are expecting a score with 8 quarter notes: C4, D4, E4, F4, G4, A4, B4, C5 over two measures.

            ScoreAssert.AssertSequence(score)
                .Note(PitchClass.C, SymbolicDuration.Quarter)
                .Note(PitchClass.D, SymbolicDuration.Quarter)
                .Note(PitchClass.E, SymbolicDuration.Quarter)
                .Note(PitchClass.F, SymbolicDuration.Quarter)
                .Note(PitchClass.G, SymbolicDuration.Quarter)
                .Note(PitchClass.A, SymbolicDuration.Quarter)
                .Note(PitchClass.B, SymbolicDuration.Quarter)
                .Note(PitchClass.C, SymbolicDuration.Quarter, octave: 5)
                .AndNoMore();

        }

        private async Task<NotationScore> ExtractScoreFromAudio(string testFile, AudioPipelineOptions? options = null)
        {
            using var inputStream = File.OpenRead(@$"TestData\{testFile}");

            options = (options ?? AudioPipelineOptions.Default) with
            {
                DiagnosticsCollector = new XUnitDiagnosticsCollector(outputHelper)
            };

            return await AudioPipeline.FromWavAsync(inputStream, options);
        }
    }
}
