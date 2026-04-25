namespace HotelChannelManager.Models;

public class Reservation
{
    public int Id { get; set; }
    public string ReservationType { get; set; } = "NEW";       // NEW, CHANGED, CANCELLED
    public string ProviderName { get; set; } = string.Empty;   // Sağlayıcı (OTS/MTS, ORS...)
    public string Agency { get; set; } = string.Empty;         // Acente
    public string Voucher { get; set; } = string.Empty;        // Voucher kodu
    public string Hotel { get; set; } = string.Empty;          // Otel adı
    public string GuestName { get; set; } = string.Empty;      // Misafir adı
    public string RoomType { get; set; } = string.Empty;       // Oda tipi (STD, DLX...)
    public string Pension { get; set; } = string.Empty;        // Pansiyon (AI, BB, HB...)
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public int AdultCount { get; set; } = 2;
    public int ChildCount { get; set; }
    public int BabyCount { get; set; }
    public int RoomCount { get; set; } = 1;
    public string Source { get; set; } = "DIRECT";             // Kaynak
    public string Status { get; set; } = "PENDING";            // PENDING, CONFIRMED, FAILED, CANCELLED
    public DateTime? SaleDate { get; set; }                    // Satış tarihi
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }
}

public class CreateReservationRequest
{
    public string GuestName { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public string Source { get; set; } = "DIRECT";
    public string ReservationType { get; set; } = "NEW";
    public string Agency { get; set; } = string.Empty;
    public string Voucher { get; set; } = string.Empty;
    public string Hotel { get; set; } = string.Empty;
    public string Pension { get; set; } = string.Empty;
    public int AdultCount { get; set; } = 2;
    public int ChildCount { get; set; }
    public int BabyCount { get; set; }
    public int RoomCount { get; set; } = 1;
}
