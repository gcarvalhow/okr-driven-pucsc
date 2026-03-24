using Core.Application.EventStore;
using Core.Application.ServiceLifetimes;
using Core.Domain.Events.Interfaces;
using Core.Domain.EventStore;
using Core.Domain.Primitives.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Core.Persistence.EventStore;

public class EventStore<TContext>(TContext dbContext) : IEventStore<TContext>, ITransient
    where TContext : DbContext
{
    public async Task AppendAsync<TAggregate>(StoreEvent<TAggregate> storeEvent, CancellationToken cancellationToken)
           where TAggregate : IAggregateRoot
    {
        await dbContext.Set<StoreEvent<TAggregate>>().AddAsync(storeEvent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AppendAsync<TAggregate>(Snapshot<TAggregate> snapshot, CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot
    {
        await dbContext.Set<Snapshot<TAggregate>>().AddAsync(snapshot, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<List<IDomainEvent>> GetStreamAsync<TAggregate>(Guid id, ulong version, CancellationToken cancellationToken)
        where TAggregate : IAggregateRoot
            => dbContext.Set<StoreEvent<TAggregate>>()
                .AsNoTracking()
                .Where(@event => @event.AggregateId.Equals(id))
                .Where(@event => @event.Version > version)
                .Select(@event => @event.Event)
                .ToListAsync(cancellationToken);

    public Task<Snapshot<TAggregate>?> GetSnapshotAsync<TAggregate>(Guid id, CancellationToken cancellationToken)
        where TAggregate : IAggregateRoot
            => dbContext.Set<Snapshot<TAggregate>>()
                .AsNoTracking()
                .Where(snapshot => snapshot.AggregateId.Equals(id))
                .OrderByDescending(snapshot => snapshot.Version)
                .FirstOrDefaultAsync(cancellationToken);

    public IAsyncEnumerable<Guid> StreamAggregatesId<TAggregate>()
        where TAggregate : IAggregateRoot
            => dbContext.Set<StoreEvent<TAggregate>>()
                .AsNoTracking()
                .Select(@event => @event.AggregateId)
                .Distinct()
                .AsAsyncEnumerable();

    public Task<Guid> StreamAggregateId<TAggregate>(Expression<Func<StoreEvent<TAggregate>, bool>> predicate, CancellationToken cancellationToken)
        where TAggregate : IAggregateRoot
            => dbContext.Set<StoreEvent<TAggregate>>()
                .AsNoTracking()
                .Where(predicate)
                .Select(@event => @event.AggregateId)
                .FirstOrDefaultAsync(cancellationToken);
}