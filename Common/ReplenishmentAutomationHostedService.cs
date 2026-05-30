using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Services;

namespace WMS.Common;

public sealed class ReplenishmentAutomationHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ReplenishmentAutomationHostedService> _logger;

    public ReplenishmentAutomationHostedService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<ReplenishmentAutomationHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var options = LoadOptions();
            var delay = TimeSpan.FromSeconds(options.IntervalSeconds);
            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            if (!options.Enabled)
                continue;

            try
            {
                await RunOnceAsync(options, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto-replenishment worker failed.");
            }
        }
    }

    private async Task RunOnceAsync(ReplenishmentAutomationOptions options, CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IReplenishmentAutomationService>();
        var warehouseIds = await db.Warehouses
            .AsNoTracking()
            .Where(w => w.IsActive)
            .OrderBy(w => w.WarehouseCode)
            .Select(w => w.WarehouseId)
            .ToListAsync(cancellationToken);

        foreach (var warehouseId in warehouseIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var run = await service.RunAsync(new ReplenishmentAutomationRunRequest
            {
                WarehouseId = warehouseId,
                Actor = "auto-replenishment-worker",
                AutoCreateTasks = options.AutoCreateTasks,
                MaxTasks = options.MaxTasksPerRun,
                Options = options
            });
            _logger.LogInformation(
                "Auto-replenishment run {RunCode} completed for warehouse {WarehouseId}: {Created}/{Suggested} tasks.",
                run.RunCode,
                warehouseId,
                run.CreatedTaskCount,
                run.SuggestedLineCount);
        }
    }

    private ReplenishmentAutomationOptions LoadOptions()
        => ReplenishmentAutomationOptions.Normalize(
            _configuration.GetSection(ReplenishmentAutomationOptions.SectionName).Get<ReplenishmentAutomationOptions>());
}
