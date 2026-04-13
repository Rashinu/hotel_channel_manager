namespace HotelChannelManager.Services;

using HotelChannelManager.Data;
using HotelChannelManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class ReservationWorker : BackgroundService
{
    private readonly ReservationQueue _queue;
private readonly IServiceScopeFactory _scopeFactory;
private readonly ILogger<ReservationWorker> _logger;
private readonly FakeOtaService _otaService;
private readonly FakePmsService _pmsService;

public ReservationWorker(
    ReservationQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<ReservationWorker> logger,
    FakeOtaService otaService,
    FakePmsService pmsService)
{
    _queue = queue;
    _scopeFactory = scopeFactory;
    _logger = logger;
    _otaService = otaService;
    _pmsService = pmsService;
}

protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    _logger.LogInformation("Worker başladı. Kuyruk dinleniyor...");

    while (!stoppingToken.IsCancellationRequested)
    {
        // 1. Rezervasyon kuyruğunu işle
        if (_queue.TryDequeue(out var reservation) && reservation is not null)
        {
            await ProcessReservationAsync(reservation);
        }

        // 2. PENDING mailleri tara ve convert et
        await ProcessPendingMailsAsync();

        // 2 dakika bekle
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
    }
}

private async Task ProcessPendingMailsAsync()
{
    using var scope = _scopeFactory.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var mailIntegration = scope.ServiceProvider.GetRequiredService<MailIntegrationService>();

    // PENDING mailleri bul
    var pendingMails = await context.IncomingMails
        .Where(m => m.Status == "PENDING")
        .ToListAsync();

    if (!pendingMails.Any())
    {
        _logger.LogInformation("İşlenecek mail yok.");
        return;
    }

    _logger.LogInformation(
        "{Count} adet PENDING mail bulundu.", pendingMails.Count);

    foreach (var mail in pendingMails)
    {
        await mailIntegration.ProcessMailAsync(mail, context);
    }
}

    private async Task ProcessReservationAsync(Reservation reservation)
{
    _logger.LogInformation(
        "İşleniyor: ReservationId={Id}, Guest={Guest}",
        reservation.Id, reservation.GuestName);

    const int maxRetry = 3;
    var attempt = 0;

    while (attempt < maxRetry)
    {
        attempt++;
        _logger.LogInformation(
            "Deneme {Attempt}/{Max}: ReservationId={Id}",
            attempt, maxRetry, reservation.Id);

        try
        {
            // 1. OTA'dan rezervasyon verisini al
            var otaResponse = await _otaService.GetReservationAsync(reservation);

            if (!otaResponse.IsSuccess)
            {
                var errorMessage = otaResponse.StatusCode switch
                {
                    429 => "Too Many Requests — Rate limit aşıldı",
                    503 => "OTA servisi çalışmıyor",
                    _   => otaResponse.Message
                };

                _logger.LogWarning(
                    "OTA hatası Deneme {Attempt}/{Max}: ReservationId={Id}, Hata={Error}",
                    attempt, maxRetry, reservation.Id, errorMessage);

                // 3 deneme dolmadıysa bekle ve tekrar dene
                if (attempt < maxRetry)
                {
                    _logger.LogInformation(
                        "Retry bekleniyor: ReservationId={Id}, Bekleme=3sn",
                        reservation.Id);

                    await Task.Delay(3000); // 3 saniye bekle
                    continue; // Döngünün başına dön, tekrar dene
                }

                // 3 deneme de başarısız → FAILED
                await UpdateReservationStatusAsync(reservation.Id, "FAILED", errorMessage);
                return;
            }

            // 2. PMS'e ilet
            var pmsResponse = await _pmsService.SendReservationAsync(reservation);

            if (!pmsResponse.IsSuccess)
            {
                _logger.LogWarning(
                    "PMS hatası Deneme {Attempt}/{Max}: ReservationId={Id}, Hata={Error}",
                    attempt, maxRetry, reservation.Id, pmsResponse.Message);

                if (attempt < maxRetry)
                {
                    _logger.LogInformation(
                        "Retry bekleniyor: ReservationId={Id}, Bekleme=3sn",
                        reservation.Id);

                    await Task.Delay(3000);
                    continue;
                }

                await UpdateReservationStatusAsync(
                    reservation.Id, "FAILED", pmsResponse.Message);
                return;
            }

            // 3. Her ikisi de başarılı → CONFIRMED
            await UpdateReservationStatusAsync(reservation.Id, "CONFIRMED", null);
            return; // Başarılı, döngüden çık
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Worker hata Deneme {Attempt}/{Max}: ReservationId={Id}",
                attempt, maxRetry, reservation.Id);

            if (attempt < maxRetry)
            {
                await Task.Delay(3000);
                continue;
            }

            await UpdateReservationStatusAsync(reservation.Id, "FAILED", ex.Message);
        }
    }
}

    private async Task UpdateReservationStatusAsync(
        int id, string status, string? errorMessage)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var reservation = await context.Reservations.FindAsync(id);
        if (reservation is null) return;

        reservation.Status = status;
        if (errorMessage is not null)
            reservation.ErrorMessage = errorMessage;

        await context.SaveChangesAsync();

        _logger.LogInformation(
            "Status güncellendi: ReservationId={Id}, Status={Status}",
            id, status);
    }
}