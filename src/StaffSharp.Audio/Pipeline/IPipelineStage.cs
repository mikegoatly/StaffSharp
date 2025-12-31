namespace StaffSharp.Audio.Pipeline;

/// <summary>
/// Represents a single stage in the audio processing pipeline.
/// Each stage transforms input to output and can emit diagnostics.
/// </summary>
/// <typeparam name="TInput">The input type for this stage.</typeparam>
/// <typeparam name="TOutput">The output type for this stage.</typeparam>
public interface IPipelineStage<in TInput, out TOutput>
{
    /// <summary>
    /// Gets the unique name of this pipeline stage (used for diagnostics).
    /// </summary>
    string StageName { get; }

    /// <summary>
    /// Processes the input and produces output.
    /// </summary>
    /// <param name="input">The input data for this stage.</param>
    /// <param name="context">Pipeline context containing options and diagnostics.</param>
    /// <returns>The output data from this stage.</returns>
    TOutput Process(TInput input, PipelineContext context);
}
