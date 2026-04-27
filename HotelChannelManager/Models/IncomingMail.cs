namespace HotelChannelManager.Models;

public class IncomingMail
{
    public int Id { get; set; }
    public string UniqueId { get; set; } = string.Empty;
    public string UidKey { get; set; } = string.Empty;
    public DateTime? MailDate { get; set; }
    public DateTime ReceiveDate { get; set; } = DateTime.UtcNow;
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    // Gerçek SUENO TUR mailleri body'de içerik değil, bu URL'yi taşır.
    // İçerik DigitalOcean Spaces'de HTML olarak duruyor.
    // Parse etmeden önce buradan çekilmesi gerekiyor.
    public string? ContentFileUrl { get; set; }

    public bool HasAttachments { get; set; }
    public string? AttachmentName { get; set; }
    public string? AttachmentContent { get; set; }
    public bool IsRead { get; set; }
    public string ProviderName { get; set; } = string.Empty;

    // "NEW", "CHANGED", "CANCELLED" — subject'ten tespit edilir
    public string ReservationType { get; set; } = "NEW";

    public int? CustomerId { get; set; }
    public int? ProviderId { get; set; }
    public string Status { get; set; } = "PENDING";
    public string? ConvertErrorMessage { get; set; }
    public DateTime? ConvertedAt { get; set; }
}