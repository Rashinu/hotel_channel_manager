namespace HotelChannelManager.Models;

public class ProviderIntegration
{
    public int Id { get; set; }
    public string ProviderName { get; set; } = string.Empty; // OTS/MTS, ORS vs.
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string VoucherType { get; set; } = "Full";        // Full, Operator, Client
    public int Interval { get; set; } = 2;                   // Dakika cinsinden
    public bool IsActive { get; set; } = true;

    // Hangi rezervasyon tiplerini alacak
    public bool AcceptNew { get; set; } = true;
    public bool AcceptChanged { get; set; } = true;
    public bool AcceptCancelled { get; set; } = true;
    public bool AcceptBooked { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}