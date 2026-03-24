using Core.Domain.Primitives.Interfaces;

namespace Core.Domain.Primitives;

public class Entity : IEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; protected set; }

    public bool IsDeleted { get; protected set; } = false;

    public override bool Equals(object? obj)
        => obj is Entity entity && Id.Equals(entity.Id);

    public override int GetHashCode()
        => HashCode.Combine(Id);
}