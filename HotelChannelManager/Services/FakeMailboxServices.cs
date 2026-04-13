namespace HotelChannelManager.Services;

using HotelChannelManager.Data;
using HotelChannelManager.Models;
using Microsoft.EntityFrameworkCore;

public class FakeMailboxService
{
    private readonly ILogger<FakeMailboxService> _logger;

    public FakeMailboxService(ILogger<FakeMailboxService> logger)
    {
        _logger = logger;
    }

    // Fake mailler üret — Gerçekte Outlook'tan okunur (MailKit)
    public List<IncomingMail> GetFakeMails()
    {
        return new List<IncomingMail>
        {
            // OTS/MTS'den gelen mail — PDF eki var
            new IncomingMail
            {
                From = "booking.ayt@mtsglobe.com",
                Subject = "Booking Notification - New Reservation",
                Body = "Dear Reservations, Please find below new bookings!",
                ProviderName = "OTS/MTS",
                AttachmentName = "booking_D8W9YH.pdf",
                AttachmentContent = "FAKE_PDF_CONTENT_OTS",
                ReceivedAt = DateTime.UtcNow
            },

            // ORS'tan gelen mail — PDF eki var
            new IncomingMail
            {
                From = "reservations@ors.com",
                Subject = "New Booking Confirmation",
                Body = "Please find attached reservation details.",
                ProviderName = "ORS",
                AttachmentName = "reservation_ORS123.pdf",
                AttachmentContent = "FAKE_PDF_CONTENT_ORS",
                ReceivedAt = DateTime.UtcNow
            },

            // Entegrasyonu olmayan acente
            new IncomingMail
            {
                From = "unknown@acente.com",
                Subject = "Reservation Request",
                Body = "Please confirm our reservation.",
                ProviderName = "UNKNOWN",
                AttachmentName = "reservation.pdf",
                AttachmentContent = "FAKE_PDF_CONTENT_UNKNOWN",
                ReceivedAt = DateTime.UtcNow
            }
        };
    }
}