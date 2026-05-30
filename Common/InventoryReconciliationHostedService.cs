using WMS.Services;

namespace WMS.Common;

public class InventoryReconciliationHostedService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<InventoryReconciliationHostedService> _logger;
    private readonly TimeSpan _interval;
    private readonly decimal _toleranceQty;

    public InventoryReconciliationHostedService(
        IServiceProvider sp,
        IConfiguration configuration,
        ILogger<InventoryReconciliationHostedService> logger)
    {
        _sp = sp;
        _logger = logger;
        _interval = TimeSpan.FromMinutes(configuration.GetValue("InventoryConsistency:ReconciliationIntervalMinutes", 10));
        _toleranceQty = configuration.GetValue("InventoryConsistency:ReconciliationToleranceQty", 0.0001m);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[InventoryReconciliation] Started, interval {IntervalMinutes}m", _interval.TotalMinutes);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IInventoryReconciliationService>();
                await service.RunAsync(null, _toleranceQty, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[InventoryReconciliation] Run failed");
            }

            await Task.Delay(_interval, stoppingToken);
        }
        _logger.LogInformation("[InventoryReconciliation] Stopped");
    }
}
