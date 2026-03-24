namespace Core.Domain.Events;

public abstract record Message
{
    public DateTimeOffset Timestamp { get; private init; } = DateTimeOffset.UtcNow;
}