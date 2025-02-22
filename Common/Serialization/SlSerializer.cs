﻿using System.Text.Json;

namespace OpenShock.Common.Serialization;

public static class SlSerializer
{
    private static readonly JsonSerializerOptions? DefaultSerializerSettings = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    public static T? Deserialize<T>(this string json) => JsonSerializer.Deserialize<T>(json, DefaultSerializerSettings);
    public static ValueTask<T?> DeserializeAsync<T>(this Stream stream) => JsonSerializer.DeserializeAsync<T>(stream, DefaultSerializerSettings);
    public static T? Deserialize<T>(this ReadOnlySpan<byte> data) => JsonSerializer.Deserialize<T>(data, DefaultSerializerSettings);
    
    
    public static TValue? SlDeserialize<TValue>(this JsonDocument? document)
    {
        return document is null ? default : document.Deserialize<TValue>(DefaultSerializerSettings);
    }
}