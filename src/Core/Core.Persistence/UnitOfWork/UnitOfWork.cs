using Core.Application.ServiceLifetimes;
using Core.Application.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Core.Persistence.UnitOfWork;

public class UnitOfWork<TContext>(TContext dbContext) : IUnitOfWork<TContext>, IScoped
    where TContext : DbContext
{
    private readonly DatabaseFacade _database = dbContext.Database;

    public async Task ExecuteAsync(Func<CancellationToken, Task> operationAsync, CancellationToken cancellationToken)
    {
        // Implementação real dependerá do contexto
        await operationAsync(cancellationToken);
    }
}
