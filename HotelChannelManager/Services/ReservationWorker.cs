namespace HotelChannelManager.Services;

using HotelChannelManager.Data;
using HotelChannelManager.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// BackgroundService → Uygulama çalıştığı sürece
// arka planda sürekli çalışan servis
// Gerçek sistemde: Windows Service, Linux daemon gibi düşün
public class ReservationWorker : BackgroundService
{
    private readonly ReservationQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservationWorker> _logger;

    public ReservationWorker(
        ReservationQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<ReservationWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker başladı. Kuyruk dinleniyor...");

        // Uygulama durduğunda döngü biter
        while (!stoppingToken.IsCancellationRequested)
        {
            // Kuyrukta rezervasyon var mı?
            if (_queue.TryDequeue(out var reservation) && reservation is not null)
            {
                await ProcessReservationAsync(reservation);
            }
            else
            {
                // Kuyruk boşsa 2 saniye bekle, tekrar bak
                await Task.Delay(2000, stoppingToken);
            }
        }
    }

    private async Task ProcessReservationAsync(Reservation reservation)
    {
        _logger.LogInformation(
            "İşleniyor: ReservationId={Id}, Guest={Guest}",
            reservation.Id, reservation.GuestName);

        try
        {
            // Fake PMS iletimi — Faz 5'te gerçek servis gelecek
            // Şimdilik rastgele başarılı/başarısız yapıyoruz
            await Task.Delay(1000); // PMS'e bağlanıyormuş gibi 1 sn bekle

            var random = new Random();
            var isSuccess = random.Next(1, 5) != 1; // %75 başarılı, %25 başarısız

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var dbReservation = await context.Reservations
                .FindAsync(reservation.Id);

            if (dbReservation is null) return;

            if (isSuccess)
            {
                dbReservation.Status = "CONFIRMED";
                _logger.LogInformation(
                    "PMS'e iletildi: ReservationId={Id}", reservation.Id);
            }
            else
            {
                dbReservation.Status = "FAILED";
                dbReservation.ErrorMessage = "PMS connection timeout";
                _logger.LogWarning(
                    "PMS iletimi başarısız: ReservationId={Id}", reservation.Id);
            }

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Worker hata: ReservationId={Id}", reservation.Id);
        }
    }
}