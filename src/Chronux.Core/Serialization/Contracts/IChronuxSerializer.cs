namespace Chronux.Core.Serialization.Contracts;

public interface IChronuxSerializer
{
    string Serialize(object value);
    object? Deserialize(string json, Type type);
    T? Deserialize<T>(string json);
}