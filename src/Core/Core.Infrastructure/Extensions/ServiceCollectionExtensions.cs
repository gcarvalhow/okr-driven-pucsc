using Core.Application.ServiceLifetimes;
using Core.Infrastructure.Configurations;
using Core.Shared.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;
using System.Reflection;

namespace Core.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection InstallServicesFromAssemblies(
           this IServiceCollection services,
           IConfiguration configuration,
           params Assembly[] assemblies) =>
           services.Tap(
               () => InstanceFactory
                   .CreateFromAssemblies<IServiceInstaller>(assemblies)
                   .ForEach(serviceInstaller => serviceInstaller.Install(services, configuration)));

    public static IServiceCollection InstallModulesFromAssemblies(
            this IServiceCollection services,
            IConfiguration configuration,
            params Assembly[] assemblies) =>
            services.Tap(
                () => InstanceFactory
                    .CreateFromAssemblies<IModuleInstaller>(assemblies)
                    .ForEach(moduleInstaller => moduleInstaller.Install(services, configuration)));

    public static IServiceCollection AddTransientAsMatchingInterfaces(
            this IServiceCollection services,
            Assembly assembly) =>
            services.Scan(scan =>
                scan.FromAssemblies(assembly)
                    .AddClasses(filter => filter.AssignableTo<ITransient>(), false)
                    .UsingRegistrationStrategy(RegistrationStrategy.Throw)
                    .AsMatchingInterface()
                    .WithTransientLifetime());

    public static IServiceCollection AddScopedAsMatchingInterfaces(
            this IServiceCollection services,
            Assembly assembly) =>
            services.Scan(scan =>
                scan.FromAssemblies(assembly)
                    .AddClasses(filter => filter.AssignableTo<IScoped>(), false)
                    .UsingRegistrationStrategy(RegistrationStrategy.Throw)
                    .AsMatchingInterface()
                    .WithScopedLifetime());

    public static IServiceCollection AddSingletonAsMatchingInterfaces(
            this IServiceCollection services,
            Assembly assembly) =>
            services.Scan(scan =>
                scan.FromAssemblies(assembly)
                    .AddClasses(filter => filter.AssignableTo<ISingleton>(), false)
                    .UsingRegistrationStrategy(RegistrationStrategy.Throw)
                    .AsMatchingInterface()
                    .WithSingletonLifetime());
}