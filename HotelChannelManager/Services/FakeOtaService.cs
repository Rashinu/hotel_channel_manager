namespace HotelChannelManager.Services;

using HotelChannelManager.Models;

public class FakeOtaService
{
    private readonly ILogger<FakeOtaService> _logger;
    private int _requestCount = 0; // Kaç istek geldi sayıyoruz

    public FakeOtaService(ILogger<FakeOtaService> logger)
    {
        _logger = logger;
    }

    public async Task<OtaResponse> GetReservationAsync(Reservation reservation)
    {
        _requestCount++;

        _logger.LogInformation(
            "OTA isteği: ReservationId={Id}, RequestCount={Count}",
            reservation.Id, _requestCount);

        // Gerçek OTA gibi davran — biraz beklet
        await Task.Delay(500);

        // 429 — Too Many Request (ORS Rate Limit!)
        // Her 4 istekte bir rate limit hatası
        if (_requestCount % 4 == 0)
        {
            _logger.LogWarning(
                "OTA 429 Too Many Request: ReservationId={Id}", reservation.Id);

            return new OtaResponse
            {
                IsSuccess = false,
                StatusCode = 429,
                Message = "Too Many Requests. Rate limit exceeded."
            };
        }

        // 503 — Service Unavailable
        // Rastgele %20 ihtimalle servis çökmüş
        var random = new Random();
        if (random.Next(1, 6) == 1)
        {
            _logger.LogWarning(
                "OTA 503 Service Unavailable: ReservationId={Id}", reservation.Id);

            return new OtaResponse
            {
                IsSuccess = false,
                StatusCode = 503,
                Message = "Service temporarily unavailable."
            };
        }

        // Mapping hatası — %10 ihtimalle yanlış format
        if (random.Next(1, 11) == 1)
        {
            _logger.LogWarning(
                "OTA Mapping Hatası: ReservationId={Id}", reservation.Id);

            return new OtaResponse
            {
                IsSuccess = false,
                StatusCode = 200,
                Message = "Mapping error: AccommodationType not recognized."
            };
        }

        // Başarılı — OTA kendi formatında veri döndürüyor
        _logger.LogInformation(
            "OTA 200 OK: ReservationId={Id}", reservation.Id);

        return new OtaResponse
        {
            IsSuccess = true,
            StatusCode = 200,
            Message = "Success",
            Data = new OtaReservationData
            {
                BookingReference = $"BK{reservation.Id:D6}",
                GuestFullName = reservation.GuestName,        // Farklı alan adı!
                AccommodationType = reservation.RoomType,     // Farklı alan adı!
                ArrivalDate = reservation.CheckIn.ToString("yyyy-MM-dd"),
                DepartureDate = reservation.CheckOut.ToString("yyyy-MM-dd")
            }
        };
    }
}