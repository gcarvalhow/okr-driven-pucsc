namespace Core.Domain.Exceptions;

public sealed class ForbiddenException()
    : Exception("You do not have permission to access this resource.");
