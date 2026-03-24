using MongoDB.Driver;

namespace Core.Persistence.Projection;

public interface IMongoDbContext
{
    IMongoClient MongoClient { get; }
    IMongoCollection<T> GetCollection<T>();
}
