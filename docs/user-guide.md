# Khaos.Pipeline – User Guide

This package provides the default implementation for building and executing data transformation pipelines in .NET.

## Installation

```bash
dotnet add package KhaosCode.Pipeline
```

## Overview

Khaos.Pipeline implements the interfaces from `KhaosCode.Pipeline.Abstractions`, providing:
- `ProcessingPipeline<TIn, TOut>` for executing pipelines
- `PipelineBuilder<TIn, TCurrent>` for constructing pipelines fluently
- `PipelineContext` for shared state
- `BatchPipelineExecutor<TIn, TOut>` for batch processing
- `FlowPipelineStep` adapter for running flows within pipelines

## Quick Start

```csharp
using Khaos.Pipeline;
using Khaos.Pipeline.Abstractions;

// 1. Define steps
public class ValidateStep : IPipelineStep<RawOrder, RawOrder>
{
    public ValueTask<StepOutcome<RawOrder>> InvokeAsync(
        RawOrder input, IPipelineContext context, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(input.CustomerId))
            return ValueTask.FromResult(StepOutcome<RawOrder>.Abort());
            
        return ValueTask.FromResult(StepOutcome<RawOrder>.Continue(input));
    }
}

public class EnrichStep : IPipelineStep<RawOrder, EnrichedOrder>
{
    public async ValueTask<StepOutcome<EnrichedOrder>> InvokeAsync(
        RawOrder input, IPipelineContext context, CancellationToken ct)
    {
        var customer = await _db.GetCustomerAsync(input.CustomerId, ct);
        var enriched = new EnrichedOrder(input, customer);
        return StepOutcome<EnrichedOrder>.Continue(enriched);
    }
}

// 2. Build the pipeline
var pipeline = Pipeline.Start<RawOrder>()
    .UseStep(new ValidateStep())
    .UseStep(new EnrichStep())
    .UseStep(new TransformStep())
    .Build();

// 3. Process a single record
var context = new PipelineContext();
var result = await pipeline.ProcessAsync(order, context, cancellationToken);

if (result.Kind == StepOutcomeKind.Continue)
{
    var processedOrder = result.Value;
    // Use the result
}
```

## Building Pipelines

### Static Factory Methods

```csharp
// Start an identity pipeline (TIn = TCurrent)
var builder = Pipeline.Start<string>();

// Start with an initial step
var builder = Pipeline.Start<string, int>(new ParseIntStep());

// Create a single-step pipeline
var pipeline = Pipeline.FromStep(new MyStep());
```

### Fluent Builder

```csharp
var pipeline = Pipeline.Start<RawData>()
    .UseStep(new ParseStep())           // RawData → ParsedData
    .UseStep(new ValidateStep())        // ParsedData → ValidatedData
    .UseStep(new EnrichStep())          // ValidatedData → EnrichedData
    .UseStep(async (input, ctx, ct) =>  // Inline step
    {
        await SaveAsync(input);
        return StepOutcome<EnrichedData>.Continue(input);
    })
    .Build();
```

## Pipeline Context

Share state across steps:

```csharp
var context = new PipelineContext();

// Store values
context.Set("BatchId", Guid.NewGuid());
context.Set("StartTime", DateTime.UtcNow);
context.Set("Source", "kafka-orders");

// Read values in steps
public ValueTask<StepOutcome<TOut>> InvokeAsync(
    TIn input, IPipelineContext context, CancellationToken ct)
{
    // Required value (throws if missing)
    var batchId = context.Get<Guid>("BatchId");
    
    // Optional value
    if (context.TryGet<DateTime>("StartTime", out var start))
    {
        var elapsed = DateTime.UtcNow - start;
    }
    
    // Check existence
    if (context.Contains("Debug"))
    {
        // Extra logging
    }
    
    return StepOutcome<TOut>.Continue(result);
}
```

## Batch Processing

Process multiple records efficiently:

```csharp
var executor = new BatchPipelineExecutor<RawOrder, ProcessedOrder>();
var context = new PipelineContext();

// Sequential processing
var options = new PipelineExecutionOptions
{
    IsSequential = true
};

// Parallel processing (up to 4 concurrent)
var options = new PipelineExecutionOptions
{
    IsSequential = false,
    MaxDegreeOfParallelism = 4
};

await executor.ProcessBatchAsync(orders, pipeline, context, options, ct);
```

### Batch-Aware Steps

Optimize for batch operations:

```csharp
public class BulkInsertStep : IPipelineStep<Order, Order>, IBatchAwareStep<Order, Order>
{
    // Called for single records
    public ValueTask<StepOutcome<Order>> InvokeAsync(
        Order input, IPipelineContext context, CancellationToken ct)
    {
        // Single insert
        return ValueTask.FromResult(StepOutcome<Order>.Continue(input));
    }
    
    // Called when processing batches (preferred)
    public async Task<IReadOnlyList<StepOutcome<Order>>> InvokeBatchAsync(
        IReadOnlyList<Order> inputs, IPipelineContext context, CancellationToken ct)
    {
        // Bulk insert all at once
        await _db.BulkInsertAsync(inputs, ct);
        return inputs.Select(o => StepOutcome<Order>.Continue(o)).ToList();
    }
}
```

## Flow Integration

Run a flow as a pipeline step:

```csharp
using Khaos.Pipeline.Adapters;

// Create a flow
var flow = flowBuilder
    .AddStep(new ValidateStep())
    .AddStep(new ProcessStep())
    .Build();

// Wrap it as a pipeline step
var flowStep = new FlowPipelineStep<OrderInput, OrderOutput, OrderFlowContext>(
    name: "RunOrderFlow",
    flow: flow,
    executor: flowExecutor,
    contextFactory: input => new OrderFlowContext(services) { Input = input },
    resultSelector: ctx => ctx.Output
);

// Use in pipeline
var pipeline = Pipeline.Start<OrderInput>()
    .UseStep(new PreProcessStep())
    .UseStep(flowStep)  // Run flow as a step
    .UseStep(new PostProcessStep())
    .Build();
```

### Outcome Mapping

| Flow Outcome | Pipeline StepOutcome |
|--------------|---------------------|
| `FlowOutcome.Success` | `StepOutcome.Continue(result)` |
| `FlowOutcome.Failure` | `StepOutcome.Abort()` |
| Any custom outcome | `StepOutcome.Abort()` |

## Dependency Injection

```csharp
services.AddSingleton<IBatchPipelineExecutor<Order, Order>, 
    BatchPipelineExecutor<Order, Order>>();
services.AddScoped<IPipelineContext, PipelineContext>();
```

## Best Practices

1. **Keep steps small** – Each step should do one transformation.
2. **Use meaningful abort** – Abort means "skip this record", not "error".
3. **Implement IBatchAwareStep** – For DB operations, HTTP calls, etc.
4. **Reuse context keys** – Define constants for common keys.
5. **Handle cancellation** – Always check and respect `CancellationToken`.

## Related Packages

- **KhaosCode.Pipeline.Abstractions** – Core interfaces.
- **KhaosCode.Flow** – Flow implementation.
- **KhaosCode.Processing.Pipelines** – Extended implementation with metrics.
