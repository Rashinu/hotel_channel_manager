namespace HotelChannelManager.Services;

using HotelChannelManager.Models;

public class FakePmsService
{
    private readonly ILogger<FakePmsService> _logger;

    public FakePmsService(ILogger<FakePmsService> logger)
    {
        _logger = logger;
    }

    public async Task<PmsResponse> SendReservationAsync(Reservation reservation)
    {
        _logger.LogInformation(
            "PMS'e gönderiliyor: ReservationId={Id}", reservation.Id);

        // Gerçek PMS gibi davran — biraz beklet
        await Task.Delay(800);

        var random = new Random();

        // %20 — Room not found hatası
        if (random.Next(1, 6) == 1)
        {
            _logger.LogWarning(
                "PMS Room Not Found: ReservationId={Id}", reservation.Id);

            return new PmsResponse
            {
                IsSuccess = false,
                Message = "Room not found in PMS",
                ErrorCode = "ROOM_NOT_FOUND"
            };
        }

        // %10 — Timeout
        if (random.Next(1, 11) == 1)
        {
            _logger.LogWarning(
                "PMS Timeout: ReservationId={Id}", reservation.Id);

            return new PmsResponse
            {
                IsSuccess = false,
                Message = "PMS connection timeout",
                ErrorCode = "TIMEOUT"
            };
        }

        // Başarılı
        _logger.LogInformation(
            "PMS Kabul Etti: ReservationId={Id}", reservation.Id);

        return new PmsResponse
        {
            IsSuccess = true,
            Message = "Reservation accepted by PMS",
            PmsReservationId = $"PMS{reservation.Id:D8}"
        };
    }
}