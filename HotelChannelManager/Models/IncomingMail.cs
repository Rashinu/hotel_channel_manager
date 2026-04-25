namespace HotelChannelManager.Models;

public class IncomingMail
{
    public int Id { get; set; }
    public string UniqueId { get; set; } = string.Empty;       // Mail sistemindeki benzersiz ID
    public string UidKey { get; set; } = string.Empty;         // UID anahtarı
    public DateTime? MailDate { get; set; }                     // Mailin kendi tarihi
    public DateTime ReceiveDate { get; set; } = DateTime.UtcNow; // Alınma tarihi
    public string FromAddress { get; set; } = string.Empty;    // Gönderen
    public string ToAddress { get; set; } = string.Empty;      // Alıcı
    public string Subject { get; set; } = string.Empty;        // Konu
    public string Body { get; set; } = string.Empty;           // İçerik
    public bool HasAttachments { get; set; }                   // Ek var mı
    public string? AttachmentName { get; set; }                // PDF/EK adı
    public string? AttachmentContent { get; set; }             // EK içeriği (base64)
    public bool IsRead { get; set; }                           // Okundu mu
    public string ProviderName { get; set; } = string.Empty;  // Provider (OTS/MTS, ORS...)
    public int? CustomerId { get; set; }                       // Müşteri ID
    public int? ProviderId { get; set; }                       // Provider ID (FK)
    public string Status { get; set; } = "PENDING";           // PENDING, CONVERTED, FAILED
    public string? ConvertErrorMessage { get; set; }           // Dönüştürme hata mesajı
    public DateTime? ConvertedAt { get; set; }
}
