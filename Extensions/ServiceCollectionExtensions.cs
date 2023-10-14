using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PyroCache.Commands.Common;

namespace PyroCache.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddValidators(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        var validatorTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(ICommandValidator<>)))
            .Select(t => (
                serviceType: t.GetInterfaces().First(i => i.GetGenericTypeDefinition() == typeof(ICommandValidator<>))!,
                implementationType: t))
            .ToList();
        foreach (var (serviceType, implementationType) in validatorTypes)
        {
            services.AddTransient(serviceType, implementationType);
        }

        return services;
    }
}