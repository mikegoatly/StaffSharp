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
    private const int PitchWindowSamples = 2048;
    protected override string StageName => "DetectPitches";

    /// <summary>
    /// Sentinel value indicating no pitch detected (unpitched/percussive onset).
    /// </summary>
    public const int UnpitchedSentinel = -1;

    public DetectPitchesStage(AudioPipelineOptions options, IPitchDetector detector, int maxDegreeOfParallelism = 0) : base(options)
    {
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
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

        var results = new PitchDetectionResult[onsets.Length];

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

                // Center the window at the onset sample (like librosa's center=True)
                // This means half the window is before the onset, half after
                var halfWindow = PitchWindowSamples / 2;
                var windowStart = onsetSample - halfWindow;
                var windowEnd = onsetSample + halfWindow;
                
                // Clamp to valid audio boundaries
                var actualStart = Math.Max(boundaries.StartSample, windowStart);
                var actualEnd = Math.Min(boundaries.EndSample, windowEnd);
                var windowLength = actualEnd - actualStart;

                // Need at least 1024 samples (half window) for pitch detection
                // More lenient threshold helps with notes near audio boundaries
                if (windowLength < 1024)
                {
                    // Not enough samples for reliable pitch detection
                    results[i] = PitchDetectionResult.Unpitched;
                    return;
                }

                // If we need padding (window extends beyond audio boundaries), 
                // we'll need to create a padded buffer
                ReadOnlySpan<float> window;
                float[]? paddedBuffer = null;
                
                if (actualStart > windowStart || actualEnd < windowEnd)
                {
                    // Need padding (like librosa's pad_mode='constant' with zeros)
                    paddedBuffer = new float[PitchWindowSamples];
                    var paddedSpan = paddedBuffer.AsSpan();
                    
                    // Calculate where the actual audio sits in the padded buffer
                    var padLeft = actualStart - windowStart;
                    
                    // Copy the available audio data
                    var audioSlice = audio.Samples.Span.Slice(actualStart, windowLength);
                    audioSlice.CopyTo(paddedSpan.Slice(padLeft, windowLength));
                    
                    // Rest is already zeros
                    window = paddedSpan;
                }
                else
                {
                    // No padding needed
                    window = audio.Samples.Span.Slice(actualStart, PitchWindowSamples);
                }

                results[i] = _detector.DetectPitch(window, audio.SampleRate);
            }).ConfigureAwait(false);

        // Extract pitches from results
        var pitches = new int[onsets.Length];
        for (int i = 0; i < onsets.Length; i++)
        {
            pitches[i] = results[i].IsPitched
                ? MidiNote.FromFrequency(results[i].FrequencyHz).MidiNumber
                : UnpitchedSentinel;
        }

        EmitDiagnostics("Pitch count", pitches.Length);
        EmitDiagnostics("Pitches (MIDI)", pitches);
        
        // Count pitched vs unpitched
        var pitchedCount = pitches.Count(p => p != UnpitchedSentinel);
        var unpitchedCount = pitches.Length - pitchedCount;
        EmitDiagnostics("Pitched notes", pitchedCount);
        EmitDiagnostics("Unpitched/silent", unpitchedCount);
        
        // Additional diagnostics from the same detection results
        if (Options.DiagnosticsCollector != null)
        {
            var voicingProbs = new float[onsets.Length];
            var candidateCounts = new int[onsets.Length];
            
            for (int i = 0; i < onsets.Length; i++)
            {
                voicingProbs[i] = results[i].VoicingProbability;
                candidateCounts[i] = results[i].Candidates?.Count ?? 0;
            }
            
            EmitDiagnostics("Voicing probabilities", voicingProbs);
            EmitDiagnostics("Candidate counts", candidateCounts);
            
            // Report average values
            var avgVoicing = voicingProbs.Average();
            var avgCandidates = candidateCounts.Average();
            EmitDiagnostics("Avg voicing probability", avgVoicing);
            EmitDiagnostics("Avg candidates per onset", avgCandidates);
        }

        return pitches;
    }
}
