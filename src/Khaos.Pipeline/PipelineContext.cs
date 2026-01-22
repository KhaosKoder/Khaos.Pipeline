using System.Collections.Concurrent;

namespace Khaos.Pipeline;

/// <summary>
/// Thread-safe implementation of pipeline context with concurrent dictionary for shared state.
/// </summary>
public class PipelineContext : IPipelineContext
{
    private readonly ConcurrentDictionary<string, object?> _state = new();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Items => _state;

    /// <inheritdoc />
    public T Get<T>(string key)
    {
        if (!_state.TryGetValue(key, out var value))
        {
            throw new KeyNotFoundException($"Key '{key}' was not found in the context.");
        }

        if (value is T typed)
        {
            return typed;
        }

        throw new InvalidCastException($"Value for key '{key}' is not of type {typeof(T).Name}.");
    }

    /// <inheritdoc />
    public bool TryGet<T>(string key, out T? value)
    {
        if (_state.TryGetValue(key, out var obj) && obj is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc />
    public void Set(string key, object? value)
    {
        _state[key] = value;
    }

    /// <summary>
    /// Checks if the context contains the specified key.
    /// </summary>
    public bool Contains(string key)
    {
        return _state.ContainsKey(key);
    }

    /// <summary>
    /// Removes the specified key from the context.
    /// </summary>
    public bool Remove(string key)
    {
        return _state.TryRemove(key, out _);
    }

    /// <summary>
    /// Clears all entries from the context.
    /// </summary>
    public void Clear()
    {
        _state.Clear();
    }
}
