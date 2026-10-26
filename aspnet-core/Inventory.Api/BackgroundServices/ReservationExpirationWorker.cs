using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Inventory.Reservation
{
    public sealed class ReservationExpirationWorker(
      IServiceScopeFactory scopeFactory,
          IOptions<ReservationExpirationOptions> options,
         ILogger<ReservationExpirationWorker> logger)
      : BackgroundService
    {
        // Chạy ngay khi ứng dụng khởi động, sau đó lặp lại theo khoảng thời gian cấu hình.
        protected override async Task ExecuteAsync(
     CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromMinutes(
                options.Value.CheckIntervalMinutes);

            using var timer = new PeriodicTimer(interval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope =
                        scopeFactory.CreateAsyncScope();

                    var service = scope.ServiceProvider
                        .GetRequiredService<IReservationExpirationService>();

                    var expiredCount =
                        await service.ExpireReservationsAsync(stoppingToken);

                    if (expiredCount > 0)
                    {
                        logger.LogInformation(
                            "Đã xử lý {ExpiredCount} reservation hết hạn.",
                            expiredCount);
                    }
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Có lỗi khi xử lý reservation hết hạn.");
                }

                try
                {
                    await timer.WaitForNextTickAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
