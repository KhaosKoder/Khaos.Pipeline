# Khaos.Pipeline – Developer Guide

This document explains how to extend and maintain the Pipeline implementation package.

## Solution Layout

- `src/Khaos.Pipeline`: Production library implementing pipeline execution.
- `tests/Khaos.Pipeline.Tests`: xUnit test suite for all components.
- `scripts/`: PowerShell helper scripts for common workflows.
- `docs/`: Markdown documentation bundled inside the NuGet package.

## Key Types

| Type | Description |
|------|-------------|
| `ProcessingPipeline<TIn, TOut>` | Composed pipeline that executes steps sequentially |
| `PipelineBuilder<TIn, TCurrent>` | Fluent API for constructing pipelines |
| `PipelineContext` | Thread-safe shared state implementation |
| `BatchPipelineExecutor<TIn, TOut>` | Executes pipelines over batches with parallelism |
| `FlowPipelineStep<TIn, TOut>` | Adapter to run a Flow as a Pipeline step |
| `Pipeline` | Static factory methods |

## Architecture

```
Pipeline.Start<T>() → PipelineBuilder → ProcessingPipeline
                              ↓
                    IPipelineStep[] (internal)
                              ↓
              BatchPipelineExecutor (for batch processing)
```

### Processing Flow

1. `ProcessingPipeline.ProcessAsync()` iterates through steps.
2. Each step's `InvokeAsync()` is called with the current value.
3. If `StepOutcome.Continue`, the value is passed to the next step.
4. If `StepOutcome.Abort`, remaining steps are skipped.
5. Final outcome is returned.

### Batch Processing Flow

1. `BatchPipelineExecutor.ProcessBatchAsync()` receives batch + options.
2. Based on `IsSequential` and `MaxDegreeOfParallelism`:
   - Sequential: Process one record at a time.
   - Parallel: Process up to N records concurrently.
3. Each record goes through all pipeline steps.
4. `IBatchAwareStep` implementations are called with full batch when available.

## Coding Guidelines

1. **Thread Safety**
   - `PipelineContext` uses `ConcurrentDictionary` for thread-safe access.
   - Steps may run in parallel—avoid shared mutable state.

2. **ValueTask Usage**
   - `InvokeAsync` and `ProcessAsync` return `ValueTask` for hot path optimization.
   - Batch methods return `Task` (less performance-critical).

3. **Flow Adapter**
   - `FlowPipelineStep` wraps a flow to run as a pipeline step.
   - Maps `FlowOutcome.Success` → `StepOutcome.Continue`.
   - Maps other outcomes → `StepOutcome.Abort`.

4. **Builder Pattern**
   - `PipelineBuilder` is immutable—each `UseStep` returns a new builder.
   - `Build()` creates an immutable `ProcessingPipeline`.

## Testing

- Run tests: `pwsh ./scripts/Test.ps1`
- Run with coverage: `pwsh ./scripts/Test-Coverage.ps1`
- Test coverage should include:
  - Single record processing
  - Batch processing (sequential and parallel)
  - Abort scenarios
  - Context operations
  - Flow adapter behavior

## Build & Packaging

- `pwsh ./scripts/Build.ps1`: Restore + build in Release.
- `pwsh ./scripts/Clean.ps1`: Remove TestResults, artifacts.
- `pwsh ./scripts/Pack.ps1`: Create NuGet package.
- Uses **MinVer** with prefix `Khaos.Pipeline/v`.

## Dependencies

- **KhaosCode.Pipeline.Abstractions**: Core interfaces.
- **KhaosCode.Flow.Abstractions**: For flow adapter.

## Performance Considerations

1. **Struct-based StepOutcome** – Zero allocation for hot paths.
2. **ValueTask returns** – Avoids Task allocation for sync completions.
3. **Batch-aware steps** – Amortize overhead across many records.
4. **Configurable parallelism** – Balance throughput vs resource usage.

## Extending

### Adding New Step Types

```csharp
public class MyStep<TIn, TOut> : IPipelineStep<TIn, TOut>
{
    public ValueTask<StepOutcome<TOut>> InvokeAsync(
        TIn input, IPipelineContext context, CancellationToken ct)
    {
        // Transform input to output
        var output = Transform(input);
        return ValueTask.FromResult(StepOutcome<TOut>.Continue(output));
    }
}
```

### Adding Batch Optimization

```csharp
public class MyBatchStep<T> : IPipelineStep<T, T>, IBatchAwareStep<T, T>
{
    public ValueTask<StepOutcome<T>> InvokeAsync(...) { /* single */ }
    
    public async Task<IReadOnlyList<StepOutcome<T>>> InvokeBatchAsync(
        IReadOnlyList<T> inputs, IPipelineContext ctx, CancellationToken ct)
    {
        // Batch operation (e.g., bulk DB insert)
        await BulkOperationAsync(inputs, ct);
        return inputs.Select(i => StepOutcome<T>.Continue(i)).ToList();
    }
}
```
