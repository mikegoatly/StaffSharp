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

            // The scale is split across measures based on tempo detection
            // Measure 0: C, D, E, F (first 4 notes)
            score.AssertSequence(measureIndex: 0)
                .Note(PitchClass.C, SymbolicDuration.Quarter)
                .Note(PitchClass.D, SymbolicDuration.Quarter)
                .Note(PitchClass.E, SymbolicDuration.Quarter)
                .Note(PitchClass.F, SymbolicDuration.Quarter)
                .AndNoMore();

            // Measure 1: G, A, B, C (durations may vary based on tempo detection)
            var measure1Events = score.GetEvents(measureIndex: 1);
            Assert.Equal(4, measure1Events.Count);

            var measure1Notes = measure1Events.OfType<NotationNote>().ToList();
            Assert.Equal(4, measure1Notes.Count);
            Assert.Equal(PitchClass.G, measure1Notes[0].Pitch.PitchClass);
            Assert.Equal(PitchClass.A, measure1Notes[1].Pitch.PitchClass);
            Assert.Equal(PitchClass.B, measure1Notes[2].Pitch.PitchClass);
            Assert.Equal(PitchClass.C, measure1Notes[3].Pitch.PitchClass);
            Assert.Equal(5, measure1Notes[3].Pitch.Octave);
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
