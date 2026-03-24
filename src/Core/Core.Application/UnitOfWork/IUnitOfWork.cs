using Microsoft.EntityFrameworkCore;

namespace Core.Application.UnitOfWork;

public interface IUnitOfWork<TContext> where TContext : DbContext
{
    Task ExecuteAsync(Func<CancellationToken, Task> operationAsync, CancellationToken cancellationToken);
}