namespace HotelChannelManager.Pratik.Models;

// HAZIR — değiştirmene gerek yok
public class Reservation
{
    public int Id { get; set; }
    public string ReservationType { get; set; } = "NEW";
    public string ProviderName { get; set; } = string.Empty;
    public string Agency { get; set; } = string.Empty;
    public string Voucher { get; set; } = string.Empty;
    public string Hotel { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public string Pension { get; set; } = string.Empty;
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public int AdultCount { get; set; } = 2;
    public int ChildCount { get; set; }
    public int BabyCount { get; set; }
    public int RoomCount { get; set; } = 1;
    public string Source { get; set; } = "DIRECT";
    public string Status { get; set; } = "PENDING";
    public DateTime? SaleDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }
}