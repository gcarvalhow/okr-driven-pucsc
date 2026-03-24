using Core.Domain.Primitives.Interfaces;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace Core.Domain.Projection;

public interface IProjection<TProjection>
    where TProjection : IProjectionModel
{
    Task<TProjection?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default) where TId : struct;
    Task<TProjection?> FindAsync(Expression<Func<TProjection, bool>> predicate, CancellationToken cancellationToken = default);

    Task<List<TProjection>> ListAsync(CancellationToken cancellationToken = default);
    Task<List<TProjection>> ListAsync(Expression<Func<TProjection, bool>> predicate, CancellationToken cancellationToken = default);

    ValueTask ReplaceInsertAsync(TProjection replacement, CancellationToken cancellationToken = default);
    ValueTask ReplaceInsertAsync(TProjection replacement, Expression<Func<TProjection, bool>> filter, CancellationToken cancellationToken = default);

    ValueTask RebuildInsertAsync(TProjection replacement, CancellationToken cancellationToken = default);
    ValueTask RebuildInsertAsync(TProjection replacement, Expression<Func<TProjection, bool>> filter, CancellationToken cancellationToken = default);

    Task UpdateOneFieldAsync<TField, TId>(TId id, Expression<Func<TProjection, TField>> field, TField value, CancellationToken cancellationToken = default) where TId : struct;
    Task UpdateOneFieldAsync<TField>(Expression<Func<TProjection, bool>> filter, Expression<Func<TProjection, TField>> field, TField value, CancellationToken cancellationToken = default);

    Task UpdateManyFieldAsync<TField>(Expression<Func<TProjection, bool>> filter, Expression<Func<TProjection, TField>> field, TField value, CancellationToken cancellationToken = default);

    Task UpdateFieldsAsync<TId>(TId id, Action<FieldUpdateBuilder<TProjection>> buildUpdates, CancellationToken cancellationToken = default) where TId : struct;

    Task DeleteAsync<TId>(TId id, CancellationToken cancellationToken = default) where TId : struct;
    Task DeleteAsync(Expression<Func<TProjection, bool>> filter, CancellationToken cancellationToken = default);

    IMongoCollection<TProjection> GetCollection();
}