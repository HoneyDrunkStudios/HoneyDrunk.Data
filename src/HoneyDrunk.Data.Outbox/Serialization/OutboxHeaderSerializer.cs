// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;

namespace HoneyDrunk.Data.Outbox.Serialization;

/// <summary>
/// Serializes and deserializes <see cref="OutboxMessage.Headers"/>
/// as a JSON <c>Dictionary&lt;string, string&gt;</c>.
/// </summary>
public static class OutboxHeaderSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <summary>
    /// Serializes a header dictionary to a JSON string.
    /// Returns <see langword="null"/> when <paramref name="headers"/> is <see langword="null"/> or empty.
    /// </summary>
    /// <param name="headers">The headers to serialize.</param>
    /// <returns>JSON string or <see langword="null"/>.</returns>
    public static string? Serialize(IDictionary<string, string>? headers)
    {
        if (headers is null || headers.Count == 0)
            return null;

        return JsonSerializer.Serialize(headers, SerializerOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to a header dictionary.
    /// Returns <see langword="null"/> when <paramref name="json"/> is <see langword="null"/> or whitespace.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>Header dictionary or <see langword="null"/>.</returns>
    public static Dictionary<string, string>? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<Dictionary<string, string>>(json, SerializerOptions);
    }
}
