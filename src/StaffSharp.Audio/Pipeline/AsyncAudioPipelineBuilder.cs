namespace StaffSharp.Audio.Pipeline;

/// <summary>
/// Fluent builder for constructing asynchronous audio processing pipelines.
/// Allows composing multiple async stages with type safety.
/// </summary>
/// <typeparam name="TCurrent">The current output type in the pipeline chain.</typeparam>
public sealed class AsyncAudioPipelineBuilder<TCurrent>
{
    private readonly Func<AudioPipelineContext, Task<TCurrent>> _pipelineFunc;
    private readonly int _stageCount;

    internal AsyncAudioPipelineBuilder(
        Func<AudioPipelineContext, Task<TCurrent>> pipelineFunc,
        int stageCount = 0)
    {
        _pipelineFunc = pipelineFunc;
        _stageCount = stageCount;
    }

    /// <summary>
    /// Adds a new async stage to the pipeline.
    /// </summary>
    /// <typeparam name="TNext">The output type of the new stage.</typeparam>
    /// <param name="stage">The stage to add.</param>
    /// <returns>A new builder with the stage appended.</returns>
    public AsyncAudioPipelineBuilder<TNext> AddStage<TNext>(IAsyncPipelineStage<TCurrent, TNext> stage)
    {
        return new AsyncAudioPipelineBuilder<TNext>(async context =>
        {
            // Report progress before executing this stage
            context.Progress?.Report(new PipelineProgress(stage.StageName));

            var input = await _pipelineFunc(context).ConfigureAwait(false);
            return await stage.ProcessAsync(input, context).ConfigureAwait(false);
        }, _stageCount + 1);
    }

    /// <summary>
    /// Builds the final pipeline function.
    /// </summary>
    /// <returns>A function that executes the entire pipeline given a context.</returns>
    public Func<AudioPipelineContext, Task<TCurrent>> Build()
    {
        return _pipelineFunc;
    }

    /// <summary>
    /// Executes the pipeline with the given context.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    /// <returns>A task representing the final output of the pipeline.</returns>
    public Task<TCurrent> ExecuteAsync(AudioPipelineContext context)
    {
        return _pipelineFunc(context);
    }

    /// <summary>
    /// Gets the total number of stages in this pipeline.
    /// </summary>
    internal int StageCount => _stageCount;
}

/// <summary>
/// Entry point for creating asynchronous audio processing pipelines.
/// </summary>
public static class AsyncAudioPipeline
{
    /// <summary>
    /// Creates a new async pipeline starting with the given input value.
    /// </summary>
    /// <typeparam name="TInput">The input type.</typeparam>
    /// <param name="input">The initial input value.</param>
    /// <returns>An async pipeline builder.</returns>
    public static AsyncAudioPipelineBuilder<TInput> Create<TInput>(TInput input)
    {
        return new AsyncAudioPipelineBuilder<TInput>(_ => Task.FromResult(input), 0);
    }

    /// <summary>
    /// Creates a new async pipeline starting with a factory function.
    /// Useful when the input depends on the pipeline context.
    /// </summary>
    /// <typeparam name="TInput">The input type.</typeparam>
    /// <param name="inputFactory">Factory function that produces the initial input.</param>
    /// <returns>An async pipeline builder.</returns>
    public static AsyncAudioPipelineBuilder<TInput> Create<TInput>(Func<AudioPipelineContext, TInput> inputFactory)
    {
        return new AsyncAudioPipelineBuilder<TInput>(
            context => Task.FromResult(inputFactory(context)),
            0
        );
    }

    /// <summary>
    /// Creates a new async pipeline starting with an async factory function.
    /// Useful when the input depends on the pipeline context and requires async operations.
    /// </summary>
    /// <typeparam name="TInput">The input type.</typeparam>
    /// <param name="inputFactory">Async factory function that produces the initial input.</param>
    /// <returns>An async pipeline builder.</returns>
    public static AsyncAudioPipelineBuilder<TInput> Create<TInput>(Func<AudioPipelineContext, Task<TInput>> inputFactory)
    {
        return new AsyncAudioPipelineBuilder<TInput>(inputFactory, 0);
    }
}
