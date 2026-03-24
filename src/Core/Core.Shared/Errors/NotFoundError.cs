namespace Core.Shared.Errors;

public sealed class NotFoundError(Error error) : Error(error.Code, error.Message) { }