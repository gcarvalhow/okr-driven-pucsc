using Microsoft.EntityFrameworkCore;

namespace Web.StartupTasks;

internal sealed class MigrateDatabaseStartupTask(IServiceProvider provider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using IServiceScope scope = provider.CreateScope();
    }

    private static async Task MigrateDatabaseAsync<TDbContext>(IServiceScope scope, CancellationToken cancellationToken)
        where TDbContext : DbContext
    {
        TDbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        if (dbContext.Database.GetMigrations().Any())
            await dbContext.Database.MigrateAsync(cancellationToken);
        else
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }
}