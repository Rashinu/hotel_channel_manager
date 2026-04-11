namespace HotelChannelManager.Services;

// OTA'dan gelen cevabı temsil eder
public class OtaResponse
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public OtaReservationData? Data { get; set; }
}

// OTA'dan gelen rezervasyon verisi
// Her OTA farklı format gönderir — normalizasyon burada devreye girer
public class OtaReservationData
{
    public string BookingReference { get; set; } = string.Empty;
    public string GuestFullName { get; set; } = string.Empty;    // Booking.com böyle gönderiyor
    public string AccommodationType { get; set; } = string.Empty; // Bizim RoomType değil!
    public string ArrivalDate { get; set; } = string.Empty;      // DateTime değil, string!
    public string DepartureDate { get; set; } = string.Empty;
}