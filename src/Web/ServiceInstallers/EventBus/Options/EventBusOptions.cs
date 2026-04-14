using System.ComponentModel.DataAnnotations;

namespace Web.ServiceInstallers.EventBus.Options;

public sealed record EventBusOptions
{
    [Required]
    public required string ConnectionName { get; init; }

    [Required]
    public required Uri ConnectionString { get; init; }

    [Required, Range(1, 10)]
    public int RetryLimit { get; init; } = 5;

    [Required]
    public TimeSpan InitialInterval { get; init; } = TimeSpan.FromSeconds(2);

    [Required]
    public TimeSpan IntervalIncrement { get; init; } = TimeSpan.FromSeconds(5);
}