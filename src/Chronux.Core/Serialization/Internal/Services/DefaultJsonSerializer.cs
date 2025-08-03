using System.Text.Json;
using Chronux.Core.Serialization.Contracts;

namespace Chronux.Core.Serialization.Internal.Services;

internal sealed class JsonChronuxSerializer : IChronuxSerializer
{
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public string Serialize(object value) =>
        JsonSerializer.Serialize(value, value.GetType(), _options);

    public object? Deserialize(string json, Type type) =>
        JsonSerializer.Deserialize(json, type, _options);

    public T? Deserialize<T>(string json)=>
        JsonSerializer.Deserialize<T>(json, _options);
}