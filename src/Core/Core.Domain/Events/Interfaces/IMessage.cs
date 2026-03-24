namespace Core.Domain.Events.Interfaces;

public interface IMessage
{
    DateTimeOffset Timestamp { get; }
}