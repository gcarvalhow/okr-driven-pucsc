using Core.Infrastructure.Configurations;
using Web.StartupTasks;

namespace Web.ServiceInstallers.StartupTasks;

internal sealed class StartupTasksServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHostedService<MigrateDatabaseStartupTask>();
    }
}