using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ConfigHelper _configHelper;
    private readonly WindowsServiceManager _serviceManager;

    public Worker(ILogger<Worker> logger, ConfigHelper configHelper, WindowsServiceManager serviceManager)
    {
        _logger = logger;
        _configHelper = configHelper;
        _serviceManager = serviceManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

        var services = _configHelper.GetServiceNames();

        foreach (var service in services)
        {
            _logger.LogInformation("Processing service: {service}", service);

            _serviceManager.StopService(service);
            await Task.Delay(1000, stoppingToken);

            _serviceManager.StartService(service);
            await Task.Delay(1000, stoppingToken);
        }

        _logger.LogInformation("Work completed");
    }
}
