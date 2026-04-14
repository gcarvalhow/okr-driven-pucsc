using Newtonsoft.Json;

namespace Core.Infrastructure.JsonConverters;

public class TypeNameHandlingConverter : JsonConverter
{
    private readonly TypeNameHandling _typeNameHandling;

    public TypeNameHandlingConverter(TypeNameHandling typeNameHandling)
    {
        _typeNameHandling = typeNameHandling;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
            => new JsonSerializer { TypeNameHandling = _typeNameHandling }.Serialize(writer, value);

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        => new JsonSerializer { TypeNameHandling = _typeNameHandling }.Deserialize(reader, objectType);

    public override bool CanConvert(Type objectType)
        => IsMassTransitOrSystemType(objectType);

    private static bool IsMassTransitOrSystemType(Type objectType)
        => (objectType.Assembly.FullName?.Contains(nameof(MassTransit)) ?? false) ||
           objectType.Assembly.IsDynamic ||
           objectType.Assembly == typeof(object).Assembly;
}