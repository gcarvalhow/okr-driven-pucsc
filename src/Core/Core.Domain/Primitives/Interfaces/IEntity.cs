namespace Core.Domain.Primitives.Interfaces;

public interface IEntity
{
    Guid Id { get; }
    DateTimeOffset CreatedAt { get; }
    DateTimeOffset? UpdatedAt { get; }
    bool IsDeleted { get; }
}