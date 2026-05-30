using WMS.Services;

namespace WMS.Common;

public class InventorySnapshotOutboxHostedService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<InventorySnapshotOutboxHostedService> _logger;
    private readonly TimeSpan _interval;
    private readonly int _batchSize;

    public InventorySnapshotOutboxHostedService(
        IServiceProvider sp,
        IConfiguration configuration,
        ILogger<InventorySnapshotOutboxHostedService> logger)
    {
        _sp = sp;
        _logger = logger;
        _interval = TimeSpan.FromSeconds(configuration.GetValue("InventoryConsistency:OutboxIntervalSeconds", 30));
        _batchSize = configuration.GetValue("InventoryConsistency:OutboxBatchSize", 100);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[InventorySnapshotOutbox] Started, interval {IntervalSeconds}s", _interval.TotalSeconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IInventorySnapshotService>();
                await service.ProcessOutboxBatchAsync(_batchSize, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[InventorySnapshotOutbox] Batch failed");
            }

            await Task.Delay(_interval, stoppingToken);
        }
        _logger.LogInformation("[InventorySnapshotOutbox] Stopped");
    }
}
