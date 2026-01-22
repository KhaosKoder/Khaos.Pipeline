using Khaos.Flow.Abstractions;
using Khaos.Pipeline.Abstractions;
using Khaos.Pipeline.Adapters;
using Xunit;

namespace Khaos.Pipeline.Tests;

public class FlowPipelineStepTests
{
    [Fact]
    public async Task InvokeAsync_ReturnsSuccess_WhenFlowCompletes()
    {
        var executor = new TestFlowExecutor(FlowOutcome.Success);
        var flow = new TestFlowDefinition();
        var step = new FlowPipelineStep<int, string, TestFlowContext>(
            executor,
            flow,
            input => new TestFlowContext { InputValue = input },
            ctx => $"Processed: {ctx.InputValue}");

        var context = new PipelineContext();
        var outcome = await step.InvokeAsync(42, context, CancellationToken.None);

        Assert.Equal(StepOutcomeKind.Continue, outcome.Kind);
        Assert.Equal("Processed: 42", outcome.Value);
    }

    [Fact]
    public async Task InvokeAsync_ReturnsAbort_WhenFlowFails()
    {
        var executor = new TestFlowExecutor(FlowOutcome.Failure);
        var flow = new TestFlowDefinition();
        var step = new FlowPipelineStep<int, string, TestFlowContext>(
            executor,
            flow,
            input => new TestFlowContext { InputValue = input },
            ctx => ctx.InputValue.ToString());

        var context = new PipelineContext();
        var outcome = await step.InvokeAsync(42, context, CancellationToken.None);

        Assert.Equal(StepOutcomeKind.Abort, outcome.Kind);
    }

    private class TestFlowExecutor : IFlowExecutor<TestFlowContext>
    {
        private readonly FlowOutcome _outcome;

        public TestFlowExecutor(FlowOutcome outcome)
        {
            _outcome = outcome;
        }

        public Task<FlowOutcome> ExecuteAsync(
            IFlowDefinition<TestFlowContext> flow,
            TestFlowContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_outcome);
        }
    }

    private class TestFlowDefinition : IFlowDefinition<TestFlowContext>
    {
        public string Name => "TestFlow";
        public string StartStepKey => "start";
        public IReadOnlyDictionary<string, IFlowStepDefinition> Steps => 
            new Dictionary<string, IFlowStepDefinition>();
    }

    private class TestFlowContext : IFlowContext
    {
        public int InputValue { get; set; }
        public IServiceProvider Services { get; } = new TestServiceProvider();

        private class TestServiceProvider : IServiceProvider
        {
            public object? GetService(Type serviceType) => null;
        }
    }
}
