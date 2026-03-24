using FluentValidation;
using System.Linq.Expressions;

namespace Core.Application.Validators;

public abstract class PagedQueryValidator<T> : AbstractValidator<T> where T : class
{
    protected const int MaxPageSize = 100;
    protected const int MinPageSize = 1;
    protected const int MinPageNumber = 1;

    protected void AddPaginationRules(
        Expression<Func<T, int>> pageNumberSelector,
        Expression<Func<T, int>> pageSizeSelector,
        int? maxPageSize = null)
    {
        var maxSize = maxPageSize ?? MaxPageSize;

        RuleFor(pageNumberSelector)
            .GreaterThanOrEqualTo(MinPageNumber)
            .WithMessage($"Page number must be greater than or equal to {MinPageNumber}");

        RuleFor(pageSizeSelector)
            .GreaterThanOrEqualTo(MinPageSize)
            .WithMessage($"Page size must be greater than or equal to {MinPageSize}")
            .LessThanOrEqualTo(maxSize)
            .WithMessage($"Page size cannot exceed {maxSize}");
    }
}