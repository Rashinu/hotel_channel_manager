namespace HotelChannelManager.Services;

using HotelChannelManager.Data;
using HotelChannelManager.Models;
using Microsoft.EntityFrameworkCore;

public class MailIntegrationService(
    ILogger<MailIntegrationService> _logger,
    PdfParserService _pdfParser)
{
    // Maili alır, parse eder, rezervasyon oluşturur.
    // Status: PENDING → CONVERTED veya FAILED
    public async Task ProcessMailAsync(IncomingMail mail, AppDbContext context)
    {
        _logger.LogInformation("Mail işleniyor: {From} | {Provider}", mail.FromAddress, mail.ProviderName);

        ParsedReservation? parsed = null;

        if (!string.IsNullOrEmpty(mail.AttachmentContent))
        {
            // PDF eki var → base64 decode → PDF parse
            var pdfBytes = Convert.FromBase64String(mail.AttachmentContent);
            parsed = _pdfParser.ParsePdf(mail.ProviderName, pdfBytes);
        }
        else
        {
            // HTML içeriği body'den al, body boşsa ContentFileUrl'den çek
            var htmlContent = mail.Body;

            if (string.IsNullOrEmpty(htmlContent) && !string.IsNullOrEmpty(mail.ContentFileUrl))
            {
                _logger.LogInformation("URL'den HTML çekiliyor: {Url}", mail.ContentFileUrl);
                using var http = new HttpClient();
                http.Timeout = TimeSpan.FromSeconds(15);
                htmlContent = await http.GetStringAsync(mail.ContentFileUrl);
            }

            if (!string.IsNullOrEmpty(htmlContent))
            {
                parsed = _pdfParser.ParseHtml(mail.ProviderName, htmlContent);

                // Provider tanınmadı ama tablolu HTML var → SUENO parser'ı dene
                if (parsed is null && htmlContent.Contains("<table", StringComparison.OrdinalIgnoreCase))
                    parsed = _pdfParser.ParseHtml("SUENO", htmlContent);
            }
        }

        if (parsed is null)
        {
            mail.Status = "FAILED";
            mail.ConvertErrorMessage = $"Parse edilemedi. Provider: {mail.ProviderName}";
            await context.SaveChangesAsync();
            return;
        }

        // Mail'den gelen tip öncelikli (subject'ten tespit edildi)
        var reservationType = mail.ReservationType != "NEW" && !string.IsNullOrEmpty(mail.ReservationType)
            ? mail.ReservationType
            : (parsed.ReservationType ?? "NEW");

        var reservation = new Reservation
        {
            ReservationType = reservationType,
            ProviderName    = mail.ProviderName,
            Voucher         = parsed.Voucher,
            GuestName       = string.IsNullOrEmpty(parsed.GuestName) ? "Unknown Guest" : parsed.GuestName,
            RoomType        = string.IsNullOrEmpty(parsed.RoomType)  ? "STANDARD"       : parsed.RoomType,
            Pension         = parsed.Pension,
            CheckIn         = parsed.CheckIn  == default ? DateOnly.FromDateTime(DateTime.Today)            : parsed.CheckIn,
            CheckOut        = parsed.CheckOut == default ? DateOnly.FromDateTime(DateTime.Today.AddDays(7)) : parsed.CheckOut,
            AdultCount      = parsed.AdultCount > 0 ? parsed.AdultCount : 2,
            ChildCount      = parsed.ChildCount,
            Source          = mail.ProviderName,
            Status          = "PENDING",
            SaleDate        = DateTime.UtcNow
        };

        context.Reservations.Add(reservation);

        mail.Status      = "CONVERTED";
        mail.IsRead      = true;
        mail.ConvertedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        _logger.LogInformation(
            "Rezervasyon oluşturuldu: MailId={Id} | Voucher={Voucher} | Guest={Guest}",
            mail.Id, reservation.Voucher, reservation.GuestName);
    }
}