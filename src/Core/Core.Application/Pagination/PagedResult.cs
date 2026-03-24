using Core.Domain.Pagination;
using Core.Domain.Pagination.Interfaces;
using System.Text.Json.Serialization;

namespace Core.Application.Pagination;

public record PagedResult<TItem>(IReadOnlyCollection<TItem> Items, Paging Paging) : IPagedResult<TItem> where TItem : class
{
    public Page Page => new Page
    {
        PageNumber = Paging.Number,
        PageSize = Paging.Size,
        HasNextPage = Items.Count > Paging.Size,
        HasPreviousPage = Paging.Number > 0
    };

    [JsonIgnore]
    private Paging Paging { get; } = Paging;

    public static IPagedResult<TItem> Create(Paging paging, IQueryable<TItem> source)
        => new PagedResult<TItem>(ApplyPagination(paging, source)?.ToList(), paging);

    private static IQueryable<TItem> ApplyPagination(Paging paging, IQueryable<TItem> source)
        => source.Skip(paging.Size * (paging.Number - 1)).Take(paging.Size + 1);
}