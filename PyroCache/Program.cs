using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
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
var hostBuilder = SuperSocketHostBuilder.Create<StringPackageInfo, CommandLinePipelineFilter>(args);
{
    hostBuilder
        .ConfigureSuperSocket(opts =>
        {
            // opts.DefaultTextEncoding = Encoding.UTF8;
            opts.AddListener(new ListenOptions
            {
                Ip = IPAddress.Loopback.ToString(),
                Port = 1001
            });
        })
        .UsePipelineFilterFactory<DefaultPipelineFilterFactory<StringPackageInfo, CommandLinePipelineFilter>>()
        .UseCommand(opts =>
        {
            opts.AddCommandAssembly(Assembly.GetExecutingAssembly());
            opts.AddGlobalCommandFilter<CommandLoggingFilterAttribute>();
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