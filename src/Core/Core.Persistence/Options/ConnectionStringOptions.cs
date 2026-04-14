namespace Core.Persistence.Options;

public sealed class ConnectionStringOptions
{
    public string Value { get; internal set; } = string.Empty;
    public static implicit operator string(ConnectionStringOptions connectionString) => connectionString.Value;
}