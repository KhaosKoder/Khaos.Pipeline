using Khaos.Pipeline.Abstractions;
using Xunit;

namespace Khaos.Pipeline.Tests;

public class PipelineBuilderTests
{
    [Fact]
    public async Task Build_CreatesPassThroughPipeline_WhenNoStepsAdded()
    {
        var pipeline = Pipeline.Start<int>().Build();
        var context = new PipelineContext();

        var outcome = await pipeline.ProcessAsync(42, context, CancellationToken.None);

        Assert.Equal(StepOutcomeKind.Continue, outcome.Kind);
        Assert.Equal(42, outcome.Value);
    }

    [Fact]
    public async Task UseStep_ChainsSteps_InOrder()
    {
        var pipeline = Pipeline.Start<int>()
            .UseStep(x => x * 2)
            .UseStep(x => x + 10)
            .Build();

        var context = new PipelineContext();
        var outcome = await pipeline.ProcessAsync(5, context, CancellationToken.None);

        Assert.Equal(StepOutcomeKind.Continue, outcome.Kind);
        Assert.Equal(20, outcome.Value); // (5 * 2) + 10 = 20
    }

    [Fact]
    public async Task UseStep_WithIPipelineStep_ExecutesCorrectly()
    {
        var step = new DoubleStep();
        var pipeline = Pipeline.Start<int>()
            .UseStep(step)
            .Build();

        var context = new PipelineContext();
        var outcome = await pipeline.ProcessAsync(21, context, CancellationToken.None);

        Assert.Equal(StepOutcomeKind.Continue, outcome.Kind);
        Assert.Equal(42, outcome.Value);
    }

    [Fact]
    public async Task UseStep_WithAsyncFunc_ExecutesCorrectly()
    {
        var pipeline = Pipeline.Start<int>()
            .UseStep(async (x, ctx, ct) =>
            {
                await Task.Delay(1, ct);
                return StepOutcome<string>.Continue(x.ToString());
            })
            .Build();

        var context = new PipelineContext();
        var outcome = await pipeline.ProcessAsync(42, context, CancellationToken.None);

        Assert.Equal(StepOutcomeKind.Continue, outcome.Kind);
        Assert.Equal("42", outcome.Value);
    }

    [Fact]
    public async Task Pipeline_PropagatesAbort_WhenStepFails()
    {
        var abortingStep = new AbortingStep<int, int>();
        var pipeline = Pipeline.Start<int>()
            .UseStep(new DoubleStep())
            .UseStep(abortingStep)
            .UseStep(new DoubleStep()) // Should not execute
            .Build();

        var context = new PipelineContext();
        var outcome = await pipeline.ProcessAsync(5, context, CancellationToken.None);

        Assert.Equal(StepOutcomeKind.Abort, outcome.Kind);
    }

    [Fact]
    public async Task UseStep_SyncFunc_CatchesException_AndReturnsAbort()
    {
        var pipeline = Pipeline.Start<int>()
            .UseStep<int>(x => throw new InvalidOperationException("Boom!"))
            .Build();

        var context = new PipelineContext();
        var outcome = await pipeline.ProcessAsync(5, context, CancellationToken.None);

        Assert.Equal(StepOutcomeKind.Abort, outcome.Kind);
    }

    [Fact]
    public async Task Pipeline_FromStep_CreatesSingleStepPipeline()
    {
        var pipeline = Pipeline.FromStep(new DoubleStep());

        var context = new PipelineContext();
        var outcome = await pipeline.ProcessAsync(7, context, CancellationToken.None);

        Assert.Equal(14, outcome.Value);
    }

    [Fact]
    public async Task Pipeline_Start_WithFirstStep_ExecutesCorrectly()
    {
        var pipeline = Pipeline.Start<int, int>(new DoubleStep())
            .UseStep(x => x + 1)
            .Build();

        var context = new PipelineContext();
        var outcome = await pipeline.ProcessAsync(10, context, CancellationToken.None);

        Assert.Equal(21, outcome.Value); // (10 * 2) + 1
    }

    private class DoubleStep : IPipelineStep<int, int>
    {
        public ValueTask<StepOutcome<int>> InvokeAsync(int input, IPipelineContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(StepOutcome<int>.Continue(input * 2));
        }
    }

    private class AbortingStep<TIn, TOut> : IPipelineStep<TIn, TOut>
    {
        public ValueTask<StepOutcome<TOut>> InvokeAsync(TIn input, IPipelineContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(StepOutcome<TOut>.Abort());
        }
    }
}
