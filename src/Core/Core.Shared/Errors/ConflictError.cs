namespace Core.Shared.Errors;

public sealed class ConflictError(Error error) : Error(error.Code, error.Message) { }