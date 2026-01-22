namespace Khaos.Pipeline;

/// <summary>
/// Fluent builder for constructing processing pipelines.
/// </summary>
/// <typeparam name="TIn">The original input type to the pipeline.</typeparam>
/// <typeparam name="TCurrent">The current output type in the pipeline chain.</typeparam>
public class PipelineBuilder<TIn, TCurrent> : IPipelineBuilder<TIn, TCurrent>
{
    private readonly Func<TIn, IPipelineContext, CancellationToken, ValueTask<StepOutcome<TCurrent>>> _chainedExecutor;

    /// <summary>
    /// Creates a builder with an initial executor.
    /// </summary>
    internal PipelineBuilder(Func<TIn, IPipelineContext, CancellationToken, ValueTask<StepOutcome<TCurrent>>> chainedExecutor)
    {
        _chainedExecutor = chainedExecutor;
    }

    /// <inheritdoc />
    public IPipelineBuilder<TIn, TNext> UseStep<TNext>(IPipelineStep<TCurrent, TNext> step)
    {
        ArgumentNullException.ThrowIfNull(step);

        return new PipelineBuilder<TIn, TNext>(async (input, context, ct) =>
        {
            var previousOutcome = await _chainedExecutor(input, context, ct).ConfigureAwait(false);

            // If previous step aborted, propagate with default output
            if (previousOutcome.Kind == StepOutcomeKind.Abort)
            {
                return StepOutcome<TNext>.Abort();
            }

            // Execute the next step
            return await step.InvokeAsync(previousOutcome.Value!, context, ct).ConfigureAwait(false);
        });
    }

    /// <summary>
    /// Adds a step using an async function delegate.
    /// </summary>
    /// <typeparam name="TNext">The output type of the step.</typeparam>
    /// <param name="stepFunc">The async function to execute.</param>
    /// <returns>A new builder for the extended pipeline.</returns>
    public PipelineBuilder<TIn, TNext> UseStep<TNext>(Func<TCurrent, IPipelineContext, CancellationToken, ValueTask<StepOutcome<TNext>>> stepFunc)
    {
        ArgumentNullException.ThrowIfNull(stepFunc);

        return new PipelineBuilder<TIn, TNext>(async (input, context, ct) =>
        {
            var previousOutcome = await _chainedExecutor(input, context, ct).ConfigureAwait(false);

            // If previous step aborted, propagate
            if (previousOutcome.Kind == StepOutcomeKind.Abort)
            {
                return StepOutcome<TNext>.Abort();
            }

            return await stepFunc(previousOutcome.Value!, context, ct).ConfigureAwait(false);
        });
    }

    /// <summary>
    /// Adds a synchronous transformation step.
    /// </summary>
    /// <typeparam name="TNext">The output type of the step.</typeparam>
    /// <param name="transformFunc">The synchronous transformation function.</param>
    /// <returns>A new builder for the extended pipeline.</returns>
    public PipelineBuilder<TIn, TNext> UseStep<TNext>(Func<TCurrent, TNext> transformFunc)
    {
        ArgumentNullException.ThrowIfNull(transformFunc);

        return new PipelineBuilder<TIn, TNext>(async (input, context, ct) =>
        {
            var previousOutcome = await _chainedExecutor(input, context, ct).ConfigureAwait(false);

            // If previous step aborted, propagate
            if (previousOutcome.Kind == StepOutcomeKind.Abort)
            {
                return StepOutcome<TNext>.Abort();
            }

            try
            {
                var result = transformFunc(previousOutcome.Value!);
                return StepOutcome<TNext>.Continue(result);
            }
            catch
            {
                return StepOutcome<TNext>.Abort();
            }
        });
    }

    /// <inheritdoc />
    public IProcessingPipeline<TIn, TCurrent> Build()
    {
        return new ProcessingPipeline<TIn, TCurrent>(_chainedExecutor);
    }
}
