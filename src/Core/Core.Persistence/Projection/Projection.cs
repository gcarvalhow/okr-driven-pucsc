using Core.Domain.Primitives.Interfaces;
using MongoDB.Driver;

namespace Core.Persistence.Projection;

public class Projection<TProjection>(IMongoDbContext context)
    where TProjection : IProjectionModel
{
    private readonly IMongoCollection<TProjection> _collection = context.GetCollection<TProjection>();

    public IMongoCollection<TProjection> GetCollection() => _collection;
}
