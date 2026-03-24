namespace Core.Domain.Primitives.Interfaces;

public interface IProjectionModel
{
    Guid Id { get; }
    DateTimeOffset CreatedAt { get; }
}