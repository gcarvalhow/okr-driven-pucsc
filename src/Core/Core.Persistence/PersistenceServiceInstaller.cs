using Core.Infrastructure.Configurations;
using Core.Infrastructure.Extensions;
using Core.Persistence.Options.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Core.Persistence;

internal sealed class PersistenceServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services
            .ConfigureOptions<ConnectionStringConfiguration>()
            .AddTransientAsMatchingInterfaces(AssemblyReference.Assembly);

        BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
    }
}