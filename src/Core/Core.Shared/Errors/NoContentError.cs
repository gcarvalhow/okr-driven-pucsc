namespace Core.Shared.Errors;

public sealed class NoContentError(Error error) : Error(error.Code, error.Message) { }