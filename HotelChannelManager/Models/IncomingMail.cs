namespace HotelChannelManager.Models;

public class IncomingMail
{
    public int Id { get; set; }
    public string From { get; set; } = string.Empty;         // Gönderen
    public string Subject { get; set; } = string.Empty;      // Konu
    public string Body { get; set; } = string.Empty;         // Mail içeriği
    public string? AttachmentName { get; set; }              // PDF/RXT adı
    public string? AttachmentContent { get; set; }           // PDF içeriği (base64)
    public string ProviderName { get; set; } = string.Empty; // Hangi provider
    public string Status { get; set; } = "PENDING";          // PENDING, CONVERTED, FAILED
    public string? ErrorMessage { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConvertedAt { get; set; }
}