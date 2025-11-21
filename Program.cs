using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<ConfigHelper>();
        services.AddSingleton<WindowsServiceManager>();
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
