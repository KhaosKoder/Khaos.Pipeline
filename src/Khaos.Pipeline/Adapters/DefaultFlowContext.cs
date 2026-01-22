using System.Collections.Concurrent;
using Khaos.Flow.Abstractions;

namespace Khaos.Pipeline.Adapters;

/// <summary>
/// Default flow context implementation backed by a pipeline context.
/// Allows flows to share state with the containing pipeline.
/// </summary>
public class DefaultFlowContext : IFlowContext
{
    private readonly ConcurrentDictionary<string, object?> _state;
    private readonly IPipelineContext? _pipelineContext;
    private readonly IServiceProvider _services;

    /// <summary>
    /// Creates a standalone flow context.
    /// </summary>
    /// <param name="services">The service provider for dependency resolution.</param>
    public DefaultFlowContext(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _state = new ConcurrentDictionary<string, object?>();
        _pipelineContext = null;
    }

    /// <summary>
    /// Creates a flow context backed by a pipeline context.
    /// State operations are delegated to the pipeline context.
    /// </summary>
    /// <param name="pipelineContext">The backing pipeline context.</param>
    /// <param name="services">The service provider for dependency resolution.</param>
    public DefaultFlowContext(IPipelineContext pipelineContext, IServiceProvider services)
    {
        _pipelineContext = pipelineContext ?? throw new ArgumentNullException(nameof(pipelineContext));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _state = new ConcurrentDictionary<string, object?>();
    }

    /// <inheritdoc />
    public IServiceProvider Services => _services;
}
