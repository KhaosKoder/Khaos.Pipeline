using Khaos.Flow.Abstractions;

namespace Khaos.Pipeline.Adapters;

/// <summary>
/// Adapter that executes a Flow as a Pipeline step.
/// Maps FlowOutcome to StepOutcome for integration.
/// </summary>
/// <typeparam name="TIn">The input type.</typeparam>
/// <typeparam name="TOut">The output type.</typeparam>
/// <typeparam name="TFlowContext">The flow context type.</typeparam>
public class FlowPipelineStep<TIn, TOut, TFlowContext> : IPipelineStep<TIn, TOut>
    where TFlowContext : class, IFlowContext
{
    private readonly IFlowExecutor<TFlowContext> _flowExecutor;
    private readonly IFlowDefinition<TFlowContext> _flowDefinition;
    private readonly Func<TIn, TFlowContext> _contextFactory;
    private readonly Func<TFlowContext, TOut> _outputExtractor;

    /// <summary>
    /// Creates a new FlowPipelineStep.
    /// </summary>
    /// <param name="flowExecutor">The executor to run the flow.</param>
    /// <param name="flowDefinition">The flow definition to execute.</param>
    /// <param name="contextFactory">Factory to create flow context from pipeline input.</param>
    /// <param name="outputExtractor">Function to extract output from flow context.</param>
    public FlowPipelineStep(
        IFlowExecutor<TFlowContext> flowExecutor,
        IFlowDefinition<TFlowContext> flowDefinition,
        Func<TIn, TFlowContext> contextFactory,
        Func<TFlowContext, TOut> outputExtractor)
    {
        _flowExecutor = flowExecutor ?? throw new ArgumentNullException(nameof(flowExecutor));
        _flowDefinition = flowDefinition ?? throw new ArgumentNullException(nameof(flowDefinition));
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _outputExtractor = outputExtractor ?? throw new ArgumentNullException(nameof(outputExtractor));
    }

    /// <inheritdoc />
    public async ValueTask<StepOutcome<TOut>> InvokeAsync(TIn input, IPipelineContext pipelineContext, CancellationToken cancellationToken = default)
    {
        var flowContext = _contextFactory(input);

        var flowOutcome = await _flowExecutor.ExecuteAsync(_flowDefinition, flowContext, cancellationToken)
            .ConfigureAwait(false);

        // FlowOutcome.Success indicates successful completion
        if (flowOutcome == FlowOutcome.Success)
        {
            return StepOutcome<TOut>.Continue(_outputExtractor(flowContext));
        }

        // Any other outcome (Failure or custom) is treated as abort
        return StepOutcome<TOut>.Abort();
    }
}
