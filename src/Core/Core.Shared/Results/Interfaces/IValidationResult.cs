using Core.Shared.Errors;

namespace Core.Shared.Results.Interfaces;

public interface IValidationResult
{
    public static readonly Error ValidationError = new("ValidationError", "A validation problem occurred.");

    Error[] Errors { get; }
}