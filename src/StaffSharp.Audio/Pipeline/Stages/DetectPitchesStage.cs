using StaffSharp;
using StaffSharp.Audio.Analysis;
using StaffSharp.Audio.Analysis.Pitch;
using StaffSharp.Audio.Analysis.Boundaries;

namespace StaffSharp.Audio.Pipeline.Stages;

/// <summary>
/// Pipeline stage that detects MIDI pitch for each onset.
/// Returns -1 for unpitched/percussive onsets.
/// </summary>
internal sealed class DetectPitchesStage : IAsyncPipelineStage<double[], int[]>
{
    private readonly IPitchDetector _detector;
    private readonly int _maxDegreeOfParallelism;
    private const int PitchWindowSamples = 4096; // Window size for pitch detection

    /// <summary>
    /// Sentinel value indicating no pitch detected (unpitched/percussive onset).
    /// </summary>
    public const int UnpitchedSentinel = -1;

    public string StageName => "DetectPitches";

    public DetectPitchesStage(IPitchDetector detector, int maxDegreeOfParallelism = 0)
    {
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
        _maxDegreeOfParallelism = maxDegreeOfParallelism; // 0 = unlimited
    }

    public async Task<int[]> ProcessAsync(double[] input, AudioPipelineContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        if (context.Audio == null)
        {
            throw new InvalidOperationException("Audio buffer not available in context.");
        }

        if (context.Boundaries == null)
        {
            throw new InvalidOperationException("Audio boundaries not available in context.");
        }

        var audio = context.Audio;
        var boundaries = context.Boundaries;
        var pitches = new int[input.Length];

        // Parallelize pitch detection for better performance
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = context.CancellationToken,
            MaxDegreeOfParallelism = _maxDegreeOfParallelism == 0 ? Environment.ProcessorCount : _maxDegreeOfParallelism
        };

        await Parallel.ForEachAsync(
            Enumerable.Range(0, input.Length),
            parallelOptions,
            async (i, ct) =>
            {
                var onsetTime = input[i];
                var onsetSample = (int)(onsetTime * audio.SampleRate);

                // Extract window around onset
                var windowStart = Math.Max(boundaries.StartSample, onsetSample);
                var windowEnd = Math.Min(boundaries.EndSample, onsetSample + PitchWindowSamples);
                var windowLength = windowEnd - windowStart;

                if (windowLength <= 0)
                {
                    pitches[i] = UnpitchedSentinel; // Invalid window = unpitched
                    return;
                }

                var window = audio.Samples.Span.Slice(windowStart, windowLength);
                var result = _detector.DetectPitch(window, audio.SampleRate);

                if (result.IsPitched)
                {
                    pitches[i] = MidiNote.FromFrequency(result.FrequencyHz).MidiNumber;
                }
                else
                {
                    // Unpitched onset (percussion, noise, or detection failure)
                    // Use sentinel value to let downstream stages decide how to handle
                    pitches[i] = UnpitchedSentinel;
                }

                await Task.Yield(); // Allow cancellation checks
            }).ConfigureAwait(false);

        context.EmitDiagnostics(StageName, "PitchCount", pitches.Length);
        context.EmitDiagnostics(StageName, "Pitches", () => pitches);

        context.Pitches = pitches;
        return pitches;
    }
}
