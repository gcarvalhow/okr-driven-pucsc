using MongoDB.Driver;

namespace Core.Persistence.Projection;

public abstract class MongoDbContext : IMongoDbContext
{
    public readonly IMongoClient _mongoClient;

    protected MongoDbContext(IMongoClient mongoClient)
    {
        _mongoClient = mongoClient;
    }

    public IMongoClient MongoClient => _mongoClient;

    public abstract IMongoCollection<T> GetCollection<T>();
}
