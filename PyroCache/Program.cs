using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PyroCache.Extensions;
using PyroCache.Filters;
using PyroCache.Jobs;
using PyroCache.Settings;
using PyroCache.Yaml;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

[assembly: InternalsVisibleTo("PyroCache.Tests")]
var hostBuilder = SuperSocketHostBuilder.Create<StringPackageInfo, CommandLinePipelineFilter>();
{
    hostBuilder
        .ConfigureSuperSocket(opts =>
        {
            opts.AddListener(new ListenOptions
            {
                Ip = "Any",
                Port = 1001
            });
        })
        .UsePipelineFilterFactory<DefaultPipelineFilterFactory<StringPackageInfo, CommandLinePipelineFilter>>()
        .UseCommand(opts =>
        {
            opts.AddCommandAssembly(Assembly.GetExecutingAssembly());
            opts.AddGlobalCommandFilter<ValidateCommandFilterAttribute>();
            opts.AddGlobalCommandFilter<CacheEntryPurgerFilterAttribute>();
        })
        .UseEnvironment(Environments.Development)
        .ConfigureServices((ctx,
                services) =>
            services
                .AddSingleton<PyroCache.PyroCache>()
                .AddSettings<CacheSettings>(ctx.Configuration)
                .AddHostedService<CachePersisterWorker>()
                .AddValidators(Assembly.GetExecutingAssembly()));

    hostBuilder
        .ConfigureHostConfiguration(builder => builder.AddYamlFile("config.yml"))
        .UseConsoleLifetime()
        .ConfigureLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddConsole();
        });
}

var host = hostBuilder.Build();
{
    await host.StartAsync();
}