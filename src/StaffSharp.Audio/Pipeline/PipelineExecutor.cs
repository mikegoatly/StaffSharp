using StaffSharp.Audio.Pipeline.Stages;
using StaffSharp.Performance;

namespace StaffSharp.Audio.Pipeline;

/// <summary>
/// Executes audio pipeline stages with support for parallelization and progress reporting.
/// Phase 1: Produces PerformanceTimeline (IR1).
/// </summary>
internal sealed class PipelineExecutor
{
    private readonly LoadAudioStage _loadAudioStage;
    private readonly DetectBoundariesStage _detectBoundariesStage;
    private readonly DetectOnsetsStage _detectOnsetsStage;
    private readonly DetectPitchesStage _detectPitchesStage;
    private readonly DetectTimeSignatureStage _detectTimeSignatureStage;
    private readonly DetectTempoStage _detectTempoStage;
    private readonly QuantizeStage _quantizeStage;
    private readonly BuildTimelineStage _buildTimelineStage;

    private const int TotalStages = 8; // Phase 1: 8 stages (no IR2 conversion yet)

    public PipelineExecutor(
        LoadAudioStage loadAudioStage,
        DetectBoundariesStage detectBoundariesStage,
        DetectOnsetsStage detectOnsetsStage,
        DetectPitchesStage detectPitchesStage,
        DetectTimeSignatureStage detectTimeSignatureStage,
        DetectTempoStage detectTempoStage,
        QuantizeStage quantizeStage,
        BuildTimelineStage buildTimelineStage)
    {
        _loadAudioStage = loadAudioStage ?? throw new ArgumentNullException(nameof(loadAudioStage));
        _detectBoundariesStage = detectBoundariesStage ?? throw new ArgumentNullException(nameof(detectBoundariesStage));
        _detectOnsetsStage = detectOnsetsStage ?? throw new ArgumentNullException(nameof(detectOnsetsStage));
        _detectPitchesStage = detectPitchesStage ?? throw new ArgumentNullException(nameof(detectPitchesStage));
        _detectTimeSignatureStage = detectTimeSignatureStage ?? throw new ArgumentNullException(nameof(detectTimeSignatureStage));
        _detectTempoStage = detectTempoStage ?? throw new ArgumentNullException(nameof(detectTempoStage));
        _quantizeStage = quantizeStage ?? throw new ArgumentNullException(nameof(quantizeStage));
        _buildTimelineStage = buildTimelineStage ?? throw new ArgumentNullException(nameof(buildTimelineStage));
    }

    /// <summary>
    /// Executes the complete audio pipeline from stream to PerformanceTimeline (IR1).
    /// Phase 1: Monophonic audio to performance timeline.
    /// </summary>
    /// <param name="stream">The input audio stream.</param>
    /// <param name="context">The pipeline context.</param>
    /// <returns>The generated performance timeline (IR1).</returns>
    public async Task<PerformanceTimeline> ExecuteAsync(Stream stream, AudioPipelineContext context)
    {
        // Create linked cancellation token source for parallel stages
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);

        try
        {
            // Stage 1: Load Audio
            ReportProgress(context, 1, _loadAudioStage.StageName);
            var audio = await _loadAudioStage.ProcessAsync(stream, context).ConfigureAwait(false);

            // Stage 2: Detect Boundaries
            ReportProgress(context, 2, _detectBoundariesStage.StageName);
            var boundaries = await _detectBoundariesStage.ProcessAsync(audio, context).ConfigureAwait(false);

            // Stage 3: Detect Onsets
            ReportProgress(context, 3, _detectOnsetsStage.StageName);
            var onsets = await _detectOnsetsStage.ProcessAsync(boundaries, context).ConfigureAwait(false);

            // Stages 4-5: Detect Pitches and Time Signatures in parallel
            // These stages are independent and can run simultaneously
            ReportProgress(context, 4, "DetectPitches & DetectTimeSignature (parallel)");

            var pitchesTask = _detectPitchesStage.ProcessAsync(onsets, context);
            var timeSignaturesTask = _detectTimeSignatureStage.ProcessAsync(onsets, context);

            // Wait for both to complete
            await Task.WhenAll(pitchesTask, timeSignaturesTask).ConfigureAwait(false);

            var pitches = await pitchesTask.ConfigureAwait(false);
            var timeSignatures = await timeSignaturesTask.ConfigureAwait(false);

            // Stage 6: Detect Tempo
            ReportProgress(context, 5, _detectTempoStage.StageName);
            var tempoMap = await _detectTempoStage.ProcessAsync(timeSignatures, context).ConfigureAwait(false);

            // Stage 7: Quantize
            ReportProgress(context, 6, _quantizeStage.StageName);
            var quantizedNotes = await _quantizeStage.ProcessAsync(tempoMap, context).ConfigureAwait(false);

            // Stage 8: Build Timeline
            ReportProgress(context, 7, _buildTimelineStage.StageName);
            var timeline = await _buildTimelineStage.ProcessAsync(quantizedNotes, context).ConfigureAwait(false);

            // Report completion
            ReportProgress(context, 8, "Complete");

            return timeline;

            // TODO (Phase 2): Add ConvertToScoreStage here to convert IR1 → IR2
        }
        catch (OperationCanceledException)
        {
            // Cancel sibling stages
            await linkedCts.CancelAsync().ConfigureAwait(false);
            throw;
        }
        catch
        {
            // Cancel sibling stages on any error
            await linkedCts.CancelAsync().ConfigureAwait(false);
            throw;
        }
    }

    private static void ReportProgress(AudioPipelineContext context, int currentStage, string stageName)
    {
        context.Progress?.Report(new PipelineProgress(currentStage, TotalStages, stageName));
    }
}
