using Chronux.Core.Configuration.Models;
using Chronux.Core.Serialization.Contracts;
using Chronux.Core.Serialization.Internal.Services;

namespace Chronux.Core.Serialization.Internal.Factories;

internal static class ChronuxSerializerFactory
{
    public static IChronuxSerializer Create(ChronuxSerializerType type) => type switch
    {
        ChronuxSerializerType.Json => new JsonChronuxSerializer(),
        ChronuxSerializerType.Binary => throw new NotImplementedException(),
        ChronuxSerializerType.Custom => throw new NotImplementedException(),
        _ => throw new NotSupportedException($"Serializer type '{type}' is not supported.")
    };
}