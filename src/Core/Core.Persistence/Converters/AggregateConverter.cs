using Core.Domain.Primitives.Interfaces;
using Core.Infrastructure.JsonConverters;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using JsonNet.ContractResolvers;
using Newtonsoft.Json;

namespace Core.Persistence.Converters;

public class AggregateConverter<TAggregate>() : ValueConverter<TAggregate?, string>(
        @event => JsonConvert.SerializeObject(@event, typeof(TAggregate), SerializerSettings()),
        jsonString => JsonConvert.DeserializeObject<TAggregate>(jsonString, DeserializerSettings()))
        where TAggregate : IAggregateRoot
{
    private static JsonSerializerSettings SerializerSettings()
    {
        JsonSerializerSettings jsonSerializerSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto
        };

        jsonSerializerSettings.Converters.Add(new DateOnlyJsonConverter());

        return jsonSerializerSettings;
    }

    private static JsonSerializerSettings DeserializerSettings()
    {
        JsonSerializerSettings jsonDeserializerSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            ContractResolver = new PrivateSetterContractResolver()
        };

        jsonDeserializerSettings.Converters.Add(new DateOnlyJsonConverter());

        return jsonDeserializerSettings;
    }
}