using StaffSharp.Audio.Pipeline;
using StaffSharp.MachineLearning;
using StaffSharp.Notation;
using StaffSharp.TestHelpers;

using Xunit.Abstractions;

namespace StaffSharp.MachineLearning.Tests.IntegrationTests;

public class MLPipelineIntegrationTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task ParsingCScale_ResultsInExpectedScore()
    {
        var score = await ExtractScoreFromAudioUsingML(@"Scales\c-scale-44100-mono.wav");

        // We are expecting a C major scale: C4, D4, E4, F4, G4, A4, B4, C5 in sequence.
        // Note: ML may detect extra notes or different durations than algorithmic approach.

        var voice = score.Parts[0].Voices[0];
        var notes = voice.Measures
            .SelectMany(m => m.Events.OfType<NotationNote>())
            .ToList();

        // Verify we detected at least 8 notes (may be more due to ML sensitivity)
        Assert.True(notes.Count >= 8, $"Expected at least 8 notes, got {notes.Count}");

        // Verify the C major scale appears in the detected notes
        var pitchClasses = notes.Select(n => n.Pitch.PitchClass).ToList();
        Assert.Contains(PitchClass.C, pitchClasses);
        Assert.Contains(PitchClass.D, pitchClasses);
        Assert.Contains(PitchClass.E, pitchClasses);
        Assert.Contains(PitchClass.F, pitchClasses);
        Assert.Contains(PitchClass.G, pitchClasses);
        Assert.Contains(PitchClass.A, pitchClasses);
        Assert.Contains(PitchClass.B, pitchClasses);

        // Verify the first and last notes are C in correct octaves
        Assert.Equal(PitchClass.C, notes[0].Pitch.PitchClass);
        Assert.Equal(4, notes[0].Pitch.Octave);
        Assert.Equal(PitchClass.C, notes[^1].Pitch.PitchClass);
        Assert.Equal(5, notes[^1].Pitch.Octave);
    }

    private async Task<NotationScore> ExtractScoreFromAudioUsingML(string testFile, AudioPipelineOptions? options = null)
    {
        using var inputStream = File.OpenRead(@$"TestData\{testFile}");

        options = (options ?? new AudioPipelineOptions()) with
        {
            NoteDetector = MLNoteDetector.Create(),
            DiagnosticsCollector = new XUnitDiagnosticsCollector(outputHelper)
        };

        return await AudioPipeline.FromWavAsync(inputStream, options);
    }
}
