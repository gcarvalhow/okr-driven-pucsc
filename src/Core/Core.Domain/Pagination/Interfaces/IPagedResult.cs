namespace Core.Domain.Pagination.Interfaces;

public interface IPagedResult<out TObject>
{
    IReadOnlyCollection<TObject> Items { get; }
    Page Page { get; }
}