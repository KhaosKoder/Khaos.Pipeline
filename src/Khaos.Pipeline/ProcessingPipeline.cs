namespace Khaos.Pipeline;

/// <summary>
/// A composed processing pipeline that executes steps in sequence.
/// </summary>
/// <typeparam name="TIn">The input type.</typeparam>
/// <typeparam name="TOut">The output type.</typeparam>
public class ProcessingPipeline<TIn, TOut> : IProcessingPipeline<TIn, TOut>
{
    private readonly Func<TIn, IPipelineContext, CancellationToken, ValueTask<StepOutcome<TOut>>> _executor;

    /// <summary>
    /// Initializes a new pipeline with the composed executor function.
    /// </summary>
    /// <param name="executor">The composed function that executes all pipeline steps.</param>
    public ProcessingPipeline(Func<TIn, IPipelineContext, CancellationToken, ValueTask<StepOutcome<TOut>>> executor)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    /// <inheritdoc />
    public ValueTask<StepOutcome<TOut>> ProcessAsync(TIn input, IPipelineContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _executor(input, context, cancellationToken);
    }
}
