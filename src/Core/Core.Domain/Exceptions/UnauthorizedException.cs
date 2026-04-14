namespace Core.Domain.Exceptions;

public sealed class UnauthorizedException()
    : Exception("Authentication is required to access this resource.");
