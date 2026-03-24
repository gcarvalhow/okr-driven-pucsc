using Core.Domain.Events.Interfaces;
using Core.Domain.EventStore;
using Core.Domain.Primitives.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Core.Application.EventStore;

public interface IEventStore<TContext> where TContext : DbContext
{
    Task AppendAsync<TAggregate>(StoreEvent<TAggregate> storeEvent, CancellationToken cancellationToken)
        where TAggregate : IAggregateRoot;

    Task AppendAsync<TAggregate>(Snapshot<TAggregate> snapshot, CancellationToken cancellationToken)
        where TAggregate : IAggregateRoot;

    Task<List<IDomainEvent>> GetStreamAsync<TAggregate>(Guid id, ulong version, CancellationToken cancellationToken)
        where TAggregate : IAggregateRoot;

    Task<Snapshot<TAggregate>?> GetSnapshotAsync<TAggregate>(Guid id, CancellationToken cancellationToken)
        where TAggregate : IAggregateRoot;

    IAsyncEnumerable<Guid> StreamAggregatesId<TAggregate>()
        where TAggregate : IAggregateRoot;

    Task<Guid> StreamAggregateId<TAggregate>(Expression<Func<StoreEvent<TAggregate>, bool>> predicate, CancellationToken cancellationToken)
        where TAggregate : IAggregateRoot;
}