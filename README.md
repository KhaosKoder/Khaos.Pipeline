# Khaos.Pipeline

Implementation of composable data transformation pipelines with batch processing support.

## Overview

This package provides the concrete implementation for processing pipelines - linear chains of transformation steps where each step transforms input to output.

## Key Types

| Type | Description |
|------|-------------|
| `PipelineBuilder<TIn, TCurrent>` | Fluent API for building pipelines |
| `ProcessingPipeline<TIn, TOut>` | Composed pipeline implementation |
| `BatchPipelineExecutor<TIn, TOut>` | Executes pipelines over batches |
| `PipelineContext` | Thread-safe shared state |
| `FlowPipelineStep<TIn, TOut>` | Adapter to run a Flow as a Pipeline step |

## Usage

```csharp
// Build a pipeline
var pipeline = Pipeline.Start<RawRecord>()
    .UseStep(new ValidateStep())
    .UseStep(new TransformStep())
    .UseStep(new EnrichStep())
    .Build();

// Process a single record
var context = new PipelineContext();
var outcome = await pipeline.ProcessAsync(input, context, cancellationToken);

// Process a batch
var executor = new BatchPipelineExecutor<RawRecord, ProcessedRecord>("MyPipeline");
await executor.ProcessBatchAsync(batch, pipeline, context, options, cancellationToken);
```

## Flow Integration

Pipelines can execute flows as steps using the `FlowPipelineStep` adapter. The flow's outcome is mapped to `StepOutcome` (Continue or Abort):

```csharp
var pipeline = Pipeline.Start<MyInput>()
    .UseStep(new PrepareStep())
    .UseStep(new FlowPipelineStep<MyInput, MyOutput, FlowContext>(flowExecutor, flow))
    .UseStep(new FinalizeStep())
    .Build();
```

## Related Packages

- `KhaosCode.Pipeline.Abstractions` - Interfaces this package implements
- `KhaosCode.Flow.Abstractions` - Flow abstractions
- `KhaosCode.Flow` - Flow implementation

## License

MIT License - see [LICENSE.md](LICENSE.md)
