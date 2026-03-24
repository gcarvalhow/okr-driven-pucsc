using MongoDB.Driver;
using System.Linq.Expressions;

namespace Core.Domain.Projection;

public class FieldUpdateBuilder<TProjection>
{
    private readonly List<UpdateDefinition<TProjection>> _updates = [];

    public FieldUpdateBuilder<TProjection> Set<TField>(Expression<Func<TProjection, TField>> field, TField value)
    {
        _updates.Add(Builders<TProjection>.Update.Set(field, value));
        return this;
    }

    public UpdateDefinition<TProjection>? Build()
        => _updates.Count > 0
            ? Builders<TProjection>.Update.Combine(_updates)
            : null;
}
