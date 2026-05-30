using WMS.Data;
using WMS.Services;

namespace WMS.Common;

/// <summary>
/// P1.2 — Background hosted service: xử lý outbox định kỳ (mỗi 30 giây).
/// Đảm bảo tất cả event được deliver đến ERP/TMS với retry logic.
/// </summary>
public class OutboxProcessorHostedService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<OutboxProcessorHostedService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    public OutboxProcessorHostedService(IServiceProvider sp, ILogger<OutboxProcessorHostedService> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[OutboxProcessor] Started — processing every {Interval}s", _interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IIntegrationService>();
                await svc.ProcessOutboxBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OutboxProcessor] Batch failed");
            }
            await Task.Delay(_interval, stoppingToken);
        }
        _logger.LogInformation("[OutboxProcessor] Stopped");
    }
}
