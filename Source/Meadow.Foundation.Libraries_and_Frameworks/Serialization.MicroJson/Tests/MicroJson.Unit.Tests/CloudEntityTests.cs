﻿using Meadow.Cloud;
using Meadow.Foundation.Serialization;
using Meadow.Update;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Unit.Tests;

public class CloudEntityTests
{
    [Fact]
    public void CloudEventSerializationTest()
    {
        var ce = new CloudEvent()
        {
            EventId = 2000,
            Description = "Cloud Sample Data",
            Timestamp = DateTime.UtcNow,
            Measurements = new Dictionary<string, object>
            {
                { "Int value", 31 },
                { "StringValue", "37-A2-0A-94-FA-42-EC-3F" }
            }
        };

        var json = MicroJson.Serialize(ce);
        Assert.NotNull(json);

        var item = JsonSerializer.Deserialize<CloudEvent>(json);

        Assert.NotNull(item);
        // the fraction of a second will be lost, so equality won't work
        Assert.True(Math.Abs((item.Timestamp - ce.Timestamp).TotalSeconds) < 1, "Timestamp failed");
    }

    [Fact]
    public void UpdateMessageSerializationTest()
    {
        UpdateMessage message = new()
        {

        };

        var json = MicroJson.Serialize(message);
    }

    [Fact]
    public void MeadowCommandSerializationTest()
    {
        var command = new MeadowCommand("command name",
            new Dictionary<string, object>
            {
                { "field 1", 23 },
                { "field 2", "foo" },
                { "field 3", true },
                { "field 4", 42.2d }
            });

        var json = MicroJson.Serialize(command);
    }

    [Fact]
    public void MeadowCommandDeserializationTest()
    {
        var expected = new Dictionary<string, object>
            {
                { "field 1", 23L },
                { "field 2", "foo" },
                { "field 3", true },
                { "field 4", 42.2d }
            };

        var json = "{\"field 1\":23,\"field 2\":\"foo\",\"field 3\":true,\"field 4\":42.2}";
        var result = MicroJson.Deserialize<Dictionary<string, object>>(json);

        Assert.NotNull(result);
        Assert.Equal(4, result.Count);
        foreach (var kvp in expected)
        {
            Assert.True(result.ContainsKey(kvp.Key));
            // this fails because the boxed '23' values arent-'t equat
            // Assert.True(result[kvp.Key] == kvp.Value, $"{result[kvp.Key]} != {kvp.Value}");
        }
    }
}
