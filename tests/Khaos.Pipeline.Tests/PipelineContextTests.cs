using Khaos.Pipeline.Abstractions;
using Xunit;

namespace Khaos.Pipeline.Tests;

public class PipelineContextTests
{
    [Fact]
    public void Get_ThrowsKeyNotFound_WhenKeyMissing()
    {
        var context = new PipelineContext();

        Assert.Throws<KeyNotFoundException>(() => context.Get<int>("missing"));
    }

    [Fact]
    public void Set_And_Get_ReturnsValue()
    {
        var context = new PipelineContext();

        context.Set("key", 42);
        var result = context.Get<int>("key");

        Assert.Equal(42, result);
    }

    [Fact]
    public void TryGet_ReturnsTrue_WhenKeyExists()
    {
        var context = new PipelineContext();
        context.Set("key", "value");

        var found = context.TryGet<string>("key", out var result);

        Assert.True(found);
        Assert.Equal("value", result);
    }

    [Fact]
    public void TryGet_ReturnsFalse_WhenKeyMissing()
    {
        var context = new PipelineContext();

        var found = context.TryGet<string>("missing", out var result);

        Assert.False(found);
        Assert.Null(result);
    }

    [Fact]
    public void Contains_ReturnsTrue_WhenKeyExists()
    {
        var context = new PipelineContext();
        context.Set("key", "value");

        Assert.True(context.Contains("key"));
    }

    [Fact]
    public void Contains_ReturnsFalse_WhenKeyMissing()
    {
        var context = new PipelineContext();

        Assert.False(context.Contains("missing"));
    }

    [Fact]
    public void Remove_ReturnsTrue_AndRemovesKey()
    {
        var context = new PipelineContext();
        context.Set("key", "value");

        var removed = context.Remove("key");

        Assert.True(removed);
        Assert.False(context.Contains("key"));
    }

    [Fact]
    public void Remove_ReturnsFalse_WhenKeyNotFound()
    {
        var context = new PipelineContext();

        Assert.False(context.Remove("missing"));
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        var context = new PipelineContext();
        context.Set("a", 1);
        context.Set("b", 2);

        context.Clear();

        Assert.False(context.Contains("a"));
        Assert.False(context.Contains("b"));
    }

    [Fact]
    public void Items_ReturnsSnapshotOfStoredValues()
    {
        var context = new PipelineContext();
        context.Set("key1", "value1");
        context.Set("key2", 42);

        var items = context.Items;

        Assert.Equal(2, items.Count);
        Assert.Equal("value1", items["key1"]);
        Assert.Equal(42, items["key2"]);
    }
}
