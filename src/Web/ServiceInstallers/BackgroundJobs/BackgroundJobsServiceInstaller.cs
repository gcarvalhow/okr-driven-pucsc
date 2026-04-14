using Core.Application.Services;
using Core.Application.Services.Interfaces;
using Core.Infrastructure.Configurations;
using Hangfire;
using Hangfire.PostgreSql;
using Newtonsoft.Json;

namespace Web.ServiceInstallers.BackgroundJobs;

internal sealed class BackgroundJobsServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfire(x =>
        {
            x.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
             .UseSimpleAssemblyNameTypeSerializer()
             .UseRecommendedSerializerSettings()
             .UseSerializerSettings(new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore })
             .UsePostgreSqlStorage(options =>
             {
                 options.UseNpgsqlConnection(configuration.GetConnectionString("Hangfire"));
             },
             new PostgreSqlStorageOptions
             {
                 SchemaName = "hangfire",
                 PrepareSchemaIfNecessary = true,
                 QueuePollInterval = TimeSpan.FromSeconds(15),
                 JobExpirationCheckInterval = TimeSpan.FromHours(12),
                 DistributedLockTimeout = TimeSpan.FromMinutes(1),
                 InvisibilityTimeout = TimeSpan.FromMinutes(5),
                 UseNativeDatabaseTransactions = true
             });
            var jsonSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            };
            x.UseSerializerSettings(jsonSettings);
        });

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = Math.Min(Environment.ProcessorCount * Convert.ToUInt16(3), Convert.ToUInt16(10));
        });

        services.AddScoped<IJobSchedulerService, JobSchedulerService>();
        services.AddScoped<MediatorHangfireBridge>();
    }
}