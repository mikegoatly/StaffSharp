namespace StaffSharp.Audio.Pipeline;

/// <summary>
/// Fluent builder for constructing audio processing pipelines.
/// Allows composing multiple stages with type safety.
/// </summary>
/// <typeparam name="TCurrent">The current output type in the pipeline chain.</typeparam>
public sealed class AudioPipelineBuilder<TCurrent>
{
    private readonly Func<PipelineContext, TCurrent> _pipelineFunc;

    internal AudioPipelineBuilder(Func<PipelineContext, TCurrent> pipelineFunc)
    {
        _pipelineFunc = pipelineFunc;
    }

    /// <summary>
    /// Adds a new stage to the pipeline.
    /// </summary>
    /// <typeparam name="TNext">The output type of the new stage.</typeparam>
    /// <param name="stage">The stage to add.</param>
    /// <returns>A new builder with the stage appended.</returns>
    public AudioPipelineBuilder<TNext> AddStage<TNext>(IPipelineStage<TCurrent, TNext> stage)
    {
        return new AudioPipelineBuilder<TNext>(context =>
        {
            var input = _pipelineFunc(context);
            return stage.Process(input, context);
        });
    }

    /// <summary>
    /// Builds the final pipeline function.
    /// </summary>
    /// <returns>A function that executes the entire pipeline given a context.</returns>
    public Func<PipelineContext, TCurrent> Build()
    {
        return _pipelineFunc;
    }

    /// <summary>
    /// Executes the pipeline with the given context.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    /// <returns>The final output of the pipeline.</returns>
    public TCurrent Execute(PipelineContext context)
    {
        return _pipelineFunc(context);
    }
}

/// <summary>
/// Entry point for creating audio processing pipelines.
/// </summary>
public static class AudioPipeline
{
    /// <summary>
    /// Creates a new pipeline starting with the given input value.
    /// </summary>
    /// <typeparam name="TInput">The input type.</typeparam>
    /// <param name="input">The initial input value.</param>
    /// <returns>A pipeline builder.</returns>
    public static AudioPipelineBuilder<TInput> Create<TInput>(TInput input)
    {
        return new AudioPipelineBuilder<TInput>(_ => input);
    }

    /// <summary>
    /// Creates a new pipeline starting with a factory function.
    /// Useful when the input depends on the pipeline context.
    /// </summary>
    /// <typeparam name="TInput">The input type.</typeparam>
    /// <param name="inputFactory">Factory function that produces the initial input.</param>
    /// <returns>A pipeline builder.</returns>
    public static AudioPipelineBuilder<TInput> Create<TInput>(Func<PipelineContext, TInput> inputFactory)
    {
        return new AudioPipelineBuilder<TInput>(inputFactory);
    }
}
