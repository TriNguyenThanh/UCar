using UCar.Interfaces;

namespace UCar.Services;

/// <summary>
/// Background service chạy định kỳ để xử lý các tác vụ tự động liên quan đến Invoice:
/// - ProcessRefundInvoicesAsync: Tự động tạo hóa đơn hoàn cọc khi đến hạn (mỗi ngày lúc 2:00 AM)
/// - CheckOverdueInvoicesAsync: Cập nhật trạng thái Overdue cho hóa đơn quá hạn (mỗi giờ)
/// </summary>
public class BackgroundInvoiceService : IHostedService, IDisposable
{
    private readonly ILogger<BackgroundInvoiceService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private Timer? _refundTimer;
    private Timer? _overdueTimer;

    public BackgroundInvoiceService(
        ILogger<BackgroundInvoiceService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BackgroundInvoiceService is starting.");

        // Timer cho ProcessRefundInvoicesAsync - chạy mỗi ngày lúc 2:00 AM
        _refundTimer = new Timer(
            ProcessRefundInvoices,
            null,
            CalculateInitialDelayForDailyTask(),
            TimeSpan.FromDays(1)); // Lặp lại mỗi 24 giờ

        // Timer cho CheckOverdueInvoicesAsync - chạy mỗi giờ
        _overdueTimer = new Timer(
            CheckOverdueInvoices,
            null,
            TimeSpan.Zero, // Chạy ngay lập tức khi start
            TimeSpan.FromHours(1)); // Lặp lại mỗi giờ

        return Task.CompletedTask;
    }

    /// <summary>
    /// Tính toán thời gian delay ban đầu để timer chạy vào lúc 2:00 AM hôm nay hoặc ngày mai
    /// </summary>
    private TimeSpan CalculateInitialDelayForDailyTask()
    {
        var now = DateTime.Now;
        var targetTime = new DateTime(now.Year, now.Month, now.Day, 2, 0, 0); // 2:00 AM hôm nay

        // Nếu đã qua 2:00 AM hôm nay, đặt target vào 2:00 AM ngày mai
        if (now > targetTime)
        {
            targetTime = targetTime.AddDays(1);
        }

        return targetTime - now;
    }

    /// <summary>
    /// Callback cho Timer - Tự động tạo hóa đơn hoàn cọc
    /// </summary>
    private void ProcessRefundInvoices(object? state)
    {
        _logger.LogInformation("ProcessRefundInvoices started at {Time}", DateTime.Now);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();

            // Chạy ProcessRefundInvoicesAsync (đồng bộ)
            // Sử dụng Task.Run.GetAwaiter().GetResult() để block cho đến khi hoàn tất
            var task = Task.Run(async () =>
            {
                try
                {
                    await invoiceService.ProcessRefundInvoicesAsync();
                    _logger.LogInformation("ProcessRefundInvoices completed successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing refund invoices.");
                }
            });
            task.GetAwaiter().GetResult(); // Wait synchronously
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in ProcessRefundInvoices callback.");
        }
    }

    /// <summary>
    /// Callback cho Timer - Cập nhật trạng thái Overdue
    /// </summary>
    private void CheckOverdueInvoices(object? state)
    {
        _logger.LogInformation("CheckOverdueInvoices started at {Time}", DateTime.Now);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();

            var task = Task.Run(async () =>
            {
                try
                {
                    await invoiceService.CheckOverdueInvoicesAsync();
                    _logger.LogInformation("CheckOverdueInvoices completed successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking overdue invoices.");
                }
            });
            task.GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in CheckOverdueInvoices callback.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BackgroundInvoiceService is stopping.");

        _refundTimer?.Change(Timeout.Infinite, 0);
        _overdueTimer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _refundTimer?.Dispose();
        _overdueTimer?.Dispose();
    }
}
