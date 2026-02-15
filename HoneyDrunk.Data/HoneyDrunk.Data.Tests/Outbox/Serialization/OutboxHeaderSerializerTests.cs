// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Outbox.Serialization;

namespace HoneyDrunk.Data.Tests.Outbox.Serialization;

public sealed class OutboxHeaderSerializerTests
{
    [Fact]
    public void Serialize_WithHeaders_ReturnsJson()
    {
        var headers = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2",
        };

        var json = OutboxHeaderSerializer.Serialize(headers);

        Assert.NotNull(json);
        Assert.Contains("key1", json);
        Assert.Contains("value1", json);
    }

    [Fact]
    public void Serialize_WithNullHeaders_ReturnsNull()
    {
        var result = OutboxHeaderSerializer.Serialize(null);

        Assert.Null(result);
    }

    [Fact]
    public void Serialize_WithEmptyHeaders_ReturnsNull()
    {
        var result = OutboxHeaderSerializer.Serialize(new Dictionary<string, string>());

        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_WithValidJson_ReturnsDictionary()
    {
        var headers = new Dictionary<string, string> { ["foo"] = "bar" };
        var json = OutboxHeaderSerializer.Serialize(headers);

        var result = OutboxHeaderSerializer.Deserialize(json);

        Assert.NotNull(result);
        Assert.Equal("bar", result["foo"]);
    }

    [Fact]
    public void Deserialize_WithNull_ReturnsNull()
    {
        var result = OutboxHeaderSerializer.Deserialize(null);

        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_WithWhitespace_ReturnsNull()
    {
        var result = OutboxHeaderSerializer.Deserialize("   ");

        Assert.Null(result);
    }

    [Fact]
    public void Roundtrip_PreservesAllHeaders()
    {
        var original = new Dictionary<string, string>
        {
            ["x-outbox-destination"] = "orders-topic",
            ["x-outbox-causation-id"] = "abc-123",
            ["x-outbox-node-id"] = "node-1",
        };

        var json = OutboxHeaderSerializer.Serialize(original);
        var restored = OutboxHeaderSerializer.Deserialize(json);

        Assert.NotNull(restored);
        Assert.Equal(original.Count, restored.Count);
        foreach (var kvp in original)
        {
            Assert.Equal(kvp.Value, restored[kvp.Key]);
        }
    }
}
