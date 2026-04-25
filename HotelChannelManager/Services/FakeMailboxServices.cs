namespace HotelChannelManager.Services;

using HotelChannelManager.Models;

public class FakeMailboxService
{
    private readonly ILogger<FakeMailboxService> _logger;

    public FakeMailboxService(ILogger<FakeMailboxService> logger)
    {
        _logger = logger;
    }

    public List<IncomingMail> GetFakeMails()
    {
        return new List<IncomingMail>
        {
            new IncomingMail
            {
                UniqueId = "20260413-001",
                UidKey = "booking.ayt@mtsglobe.com-001",
                FromAddress = "booking.ayt@mtsglobe.com",
                ToAddress = "reservation@monart.com.tr",
                Subject = "New Booking Notification 133473460 from OTS for AMTSTR221G LVD2 ref 2604GBH",
                Body = "Dear Reservations, Please find below new bookings!",
                MailDate = DateTime.UtcNow.AddMinutes(-30),
                ReceiveDate = DateTime.UtcNow.AddMinutes(-25),
                ProviderName = "OTS/MTS",
                HasAttachments = true,
                AttachmentName = "booking_D8W9YH.pdf",
                AttachmentContent = "FAKE_PDF_CONTENT_OTS",
                IsRead = false
            },
            new IncomingMail
            {
                UniqueId = "20260413-002",
                UidKey = "booking.ayt@mtsglobe.com-002",
                FromAddress = "booking.ayt@mts.com",
                ToAddress = "e.res@adalyahotel.com",
                Subject = "New Booking Notification 133473462 from OTS for ATRAYT6MQU CAHD ref 529155",
                Body = "Please find attached reservation details.",
                MailDate = DateTime.UtcNow.AddMinutes(-60),
                ReceiveDate = DateTime.UtcNow.AddMinutes(-55),
                ProviderName = "OTS/MTS",
                HasAttachments = true,
                AttachmentName = "reservation_OTS002.pdf",
                AttachmentContent = "FAKE_PDF_CONTENT_OTS2",
                IsRead = false
            },
            new IncomingMail
            {
                UniqueId = "20260413-003",
                UidKey = "noreply@w2m.com-003",
                FromAddress = "noreply@w2m.com",
                ToAddress = "reservation@cengizhotel.com",
                Subject = "New booking ('ZD3B12')",
                Body = "New booking notification.",
                MailDate = DateTime.UtcNow.AddMinutes(-5),
                ReceiveDate = DateTime.UtcNow.AddMinutes(-1),
                ProviderName = "W2M",
                HasAttachments = false,
                IsRead = false
            },
            new IncomingMail
            {
                UniqueId = "20260413-004",
                UidKey = "konfirme@ikontatil.com-004",
                FromAddress = "konfirme@ikontatil.com",
                ToAddress = "sales@myseahotel.com",
                Subject = "(Voucher #: 2379696) Ikontatil Konfirme bekleyen rezervasyonlar",
                Body = "Konfirme bekleyen rezervasyonlarınız mevcut.",
                MailDate = DateTime.UtcNow.AddMinutes(-20),
                ReceiveDate = DateTime.UtcNow.AddMinutes(-15),
                ProviderName = "IKONTATIL",
                HasAttachments = true,
                AttachmentName = "ikontatil_voucher.pdf",
                AttachmentContent = "FAKE_PDF_IKONTATIL",
                IsRead = true
            },
            new IncomingMail
            {
                UniqueId = "20260413-005",
                UidKey = "hotels@koraltravel.com-005",
                FromAddress = "hotels@koraltravel.com",
                ToAddress = "reservation@ram.com",
                Subject = "Hotel Reservation Form [Voucher:25056133-Yeni]",
                Body = "Yeni rezervasyon formu.",
                MailDate = DateTime.UtcNow.AddMinutes(-40),
                ReceiveDate = DateTime.UtcNow.AddMinutes(-35),
                ProviderName = "KORALTRAVEL",
                HasAttachments = true,
                AttachmentName = "koraltravel_form.pdf",
                AttachmentContent = "FAKE_PDF_KORAL",
                IsRead = false
            }
        };
    }
}
