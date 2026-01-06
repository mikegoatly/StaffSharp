using StaffSharp.Audio.Analysis.Boundaries;
using StaffSharp.Audio.Analysis.Pitch;

namespace StaffSharp.Audio.Pipeline.Stages;

/// <summary>
/// Pipeline stage that detects MIDI pitch for each onset.
/// Returns -1 for unpitched/percussive onsets.
/// </summary>
internal sealed class DetectPitchesStage : PipelineStageBase
{
    private readonly IPitchDetector _detector;
    private readonly int _maxDegreeOfParallelism;
    private const int PitchWindowSamples = 4096;
    protected override string StageName => "DetectPitches";

    /// <summary>
    /// Sentinel value indicating no pitch detected (unpitched/percussive onset).
    /// </summary>
    public const int UnpitchedSentinel = -1;

    public DetectPitchesStage(AudioPipelineOptions options, IPitchDetector detector, int maxDegreeOfParallelism = 0) : base(options)
    {
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
        _maxDegreeOfParallelism = maxDegreeOfParallelism; // 0 = unlimited
    }

    /// <summary>
    /// Detects MIDI pitch for each onset time.
    /// </summary>
    /// <param name="onsets">Array of onset times in seconds.</param>
    /// <param name="audio">The audio buffer to analyze.</param>
    /// <param name="boundaries">The content boundaries.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Array of MIDI pitch numbers (-1 for unpitched).</returns>
    public async Task<int[]> ExecuteAsync(
        double[] onsets,
        AudioBuffer audio,
        AudioBoundaries boundaries,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        ReportProgress("Starting pitch detection...");

        var pitches = new int[onsets.Length];

        // Parallelize pitch detection for better performance
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = ct,
            MaxDegreeOfParallelism = _maxDegreeOfParallelism == 0
                ? Environment.ProcessorCount
                : _maxDegreeOfParallelism
        };

        await Parallel.ForEachAsync(
            Enumerable.Range(0, onsets.Length),
            parallelOptions,
            async (i, ct) =>
            {
                var onsetTime = onsets[i];
                var onsetSample = (int)(onsetTime * audio.SampleRate);

                // Extract window around onset
                var windowStart = Math.Max(boundaries.StartSample, onsetSample);
                var windowEnd = Math.Min(boundaries.EndSample, onsetSample + PitchWindowSamples);
                var windowLength = windowEnd - windowStart;

                if (windowLength <= 0)
                {
                    pitches[i] = UnpitchedSentinel;
                    return;
                }

                var window = audio.Samples.Span.Slice(windowStart, windowLength);

                var result = _detector.DetectPitch(window, audio.SampleRate);

                pitches[i] = result.IsPitched
                    ? MidiNote.FromFrequency(result.FrequencyHz).MidiNumber
                    : UnpitchedSentinel;
            }).ConfigureAwait(false);

        EmitDiagnostics("Pitch count", pitches.Length);
        EmitDiagnostics("Pitches (MIDI)", pitches);
        
        // Count pitched vs unpitched
        var pitchedCount = pitches.Count(p => p != UnpitchedSentinel);
        var unpitchedCount = pitches.Length - pitchedCount;
        EmitDiagnostics("Pitched notes", pitchedCount);
        EmitDiagnostics("Unpitched/silent", unpitchedCount);

        return pitches;
    }
}
