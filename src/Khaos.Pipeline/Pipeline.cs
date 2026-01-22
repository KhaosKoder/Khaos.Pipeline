namespace Khaos.Pipeline;

/// <summary>
/// Factory methods for creating pipeline builders.
/// </summary>
public static class Pipeline
{
    /// <summary>
    /// Starts building a pipeline that accepts the specified input type.
    /// </summary>
    /// <typeparam name="T">The input type for the pipeline.</typeparam>
    /// <returns>A pipeline builder.</returns>
    public static PipelineBuilder<T, T> Start<T>()
    {
        return new PipelineBuilder<T, T>((input, _, _) => ValueTask.FromResult(StepOutcome<T>.Continue(input)));
    }

    /// <summary>
    /// Starts building a pipeline with an initial transformation step.
    /// </summary>
    /// <typeparam name="TIn">The input type.</typeparam>
    /// <typeparam name="TOut">The output type of the first step.</typeparam>
    /// <param name="step">The first step in the pipeline.</param>
    /// <returns>A pipeline builder.</returns>
    public static PipelineBuilder<TIn, TOut> Start<TIn, TOut>(IPipelineStep<TIn, TOut> step)
    {
        ArgumentNullException.ThrowIfNull(step);

        return new PipelineBuilder<TIn, TOut>((input, context, ct) => 
            step.InvokeAsync(input, context, ct));
    }

    /// <summary>
    /// Creates a pipeline from a single step.
    /// </summary>
    /// <typeparam name="TIn">The input type.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="step">The step to wrap as a pipeline.</param>
    /// <returns>A processing pipeline.</returns>
    public static IProcessingPipeline<TIn, TOut> FromStep<TIn, TOut>(IPipelineStep<TIn, TOut> step)
    {
        ArgumentNullException.ThrowIfNull(step);
        return Start(step).Build();
    }
}
