using Khaos.Pipeline.Abstractions;
using Xunit;

namespace Khaos.Pipeline.Tests;

public class BatchPipelineExecutorTests
{
    [Fact]
    public async Task ProcessBatchAsync_Returns_WhenBatchIsEmpty()
    {
        var executor = new BatchPipelineExecutor<int, int>("TestExecutor");
        var pipeline = Pipeline.Start<int>().Build();
        var context = new PipelineContext();
        var options = new PipelineExecutionOptions { IsSequential = true };

        // Should complete without error
        await executor.ProcessBatchAsync(Array.Empty<int>(), pipeline, context, options, CancellationToken.None);
    }

    [Fact]
    public async Task ProcessBatchAsync_ProcessesAllItems_Sequential()
    {
        var processed = new List<int>();
        var executor = new BatchPipelineExecutor<int, int>("TestExecutor");
        var pipeline = Pipeline.Start<int>()
            .UseStep(x =>
            {
                lock (processed) processed.Add(x);
                return x * 2;
            })
            .Build();
        var context = new PipelineContext();
        var options = new PipelineExecutionOptions { IsSequential = true };

        await executor.ProcessBatchAsync(new[] { 1, 2, 3 }, pipeline, context, options, CancellationToken.None);

        Assert.Equal(3, processed.Count);
        Assert.Equal(new[] { 1, 2, 3 }, processed);
    }

    [Fact]
    public async Task ProcessBatchAsync_ProcessesAllItems_Parallel()
    {
        var processedCount = 0;
        var executor = new BatchPipelineExecutor<int, int>("TestExecutor");
        var pipeline = Pipeline.Start<int>()
            .UseStep(x =>
            {
                Interlocked.Increment(ref processedCount);
                return x * 2;
            })
            .Build();
        var context = new PipelineContext();
        var options = new PipelineExecutionOptions
        {
            IsSequential = false,
            MaxDegreeOfParallelism = 4
        };

        await executor.ProcessBatchAsync(new[] { 1, 2, 3, 4, 5 }, pipeline, context, options, CancellationToken.None);

        Assert.Equal(5, processedCount);
    }

    [Fact]
    public void Name_ReturnsConfiguredName()
    {
        var executor = new BatchPipelineExecutor<int, int>("MyBatchExecutor");

        Assert.Equal("MyBatchExecutor", executor.Name);
    }

    private class ConditionalAbortStep : IPipelineStep<int, int>
    {
        private readonly Func<int, bool> _shouldAbort;

        public ConditionalAbortStep(Func<int, bool> shouldAbort)
        {
            _shouldAbort = shouldAbort;
        }

        public ValueTask<StepOutcome<int>> InvokeAsync(int input, IPipelineContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(_shouldAbort(input)
                ? StepOutcome<int>.Abort()
                : StepOutcome<int>.Continue(input));
        }
    }
}
