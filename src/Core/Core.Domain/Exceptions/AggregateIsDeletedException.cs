namespace Core.Domain.Exceptions;

public class AggregateIsDeletedException(Guid aggregateId) : Exception($"Aggregate '{aggregateId}' is soft-deleted and cannot be modified.") { }