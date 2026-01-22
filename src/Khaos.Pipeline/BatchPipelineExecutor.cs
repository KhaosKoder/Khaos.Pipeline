using System.Diagnostics;

namespace Khaos.Pipeline;

/// <summary>
/// Executes a pipeline over a batch of items, with optional parallelism.
/// </summary>
/// <typeparam name="TIn">The input type.</typeparam>
/// <typeparam name="TOut">The output type.</typeparam>
public class BatchPipelineExecutor<TIn, TOut> : IBatchPipelineExecutor<TIn, TOut>
{
    private readonly string _name;

    /// <summary>
    /// Initializes a new batch executor with the specified name.
    /// </summary>
    /// <param name="name">The name of this executor (for diagnostics).</param>
    public BatchPipelineExecutor(string name)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <inheritdoc />
    public async Task ProcessBatchAsync(
        IReadOnlyList<TIn> inputs,
        IProcessingPipeline<TIn, TOut> pipeline,
        IPipelineContext context,
        PipelineExecutionOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);

        if (inputs.Count == 0)
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var failedCount = 0;

        if (options.IsSequential)
        {
            // Sequential execution
            for (int i = 0; i < inputs.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var outcome = await pipeline.ProcessAsync(inputs[i], context, cancellationToken).ConfigureAwait(false);

                if (outcome.Kind == StepOutcomeKind.Abort)
                {
                    failedCount++;
                }
            }
        }
        else
        {
            // Parallel execution
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = options.MaxDegreeOfParallelism,
                CancellationToken = cancellationToken
            };

            var results = new StepOutcome<TOut>[inputs.Count];

            await Parallel.ForEachAsync(
                Enumerable.Range(0, inputs.Count),
                parallelOptions,
                async (index, ct) =>
                {
                    results[index] = await pipeline.ProcessAsync(inputs[index], context, ct).ConfigureAwait(false);
                }).ConfigureAwait(false);

            failedCount = results.Count(r => r.Kind == StepOutcomeKind.Abort);
        }

        stopwatch.Stop();
        OnBatchCompleted(inputs.Count, failedCount, stopwatch.Elapsed);
    }

    /// <summary>
    /// Called when batch processing completes. Override to add logging or metrics.
    /// </summary>
    /// <param name="totalItems">Total items processed.</param>
    /// <param name="failedItems">Number of items that aborted.</param>
    /// <param name="elapsed">Time elapsed.</param>
    protected virtual void OnBatchCompleted(int totalItems, int failedItems, TimeSpan elapsed)
    {
        // Default: no-op. Subclasses can add logging/metrics.
    }

    /// <summary>
    /// Gets the executor name.
    /// </summary>
    public string Name => _name;
}
