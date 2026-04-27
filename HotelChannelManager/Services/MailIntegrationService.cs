namespace HotelChannelManager.Services;

using HotelChannelManager.Data;
using HotelChannelManager.Models;
using Microsoft.EntityFrameworkCore;

public class MailIntegrationService(
    ILogger<MailIntegrationService> _logger,
    PdfParserService _pdfParser)
{

    public async Task ProcessMailAsync(IncomingMail mail, AppDbContext context)
    {
        _logger.LogInformation(
            "Mail işleniyor: From={From}, Provider={Provider}",
            mail.FromAddress, mail.ProviderName);

        // 1. Entegrasyon var mı kontrol et
        var integration = await context.ProviderIntegrations
            .FirstOrDefaultAsync(p =>
                p.ProviderName == mail.ProviderName && p.IsActive);

        // Entegrasyon yoksa "UNKNOWN_RESERVATION" veya "SUENO" gibi direkt parse deneriz
        // Gerçek sistemde bu kural DB'den gelir — şimdilik esnek bırakıyoruz
        if (integration is null)
        {
            _logger.LogWarning(
                "Kayıtlı entegrasyon bulunamadı, direkt parse deneniyor: Provider={Provider}",
                mail.ProviderName);
        }

        // 2. Parse stratejisine karar ver:
        //    - PDF eki varsa → PDF parse
        //    - Body HTML içeriyorsa → HTML parse
        ParsedReservation? parsed = null;

        if (!string.IsNullOrEmpty(mail.AttachmentContent))
        {
            // PDF eki var → PDF parse
            _logger.LogInformation("PDF eki parse ediliyor: MailId={Id}", mail.Id);
            var pdfBytes = Convert.FromBase64String(mail.AttachmentContent);
            parsed = _pdfParser.ParsePdf(mail.ProviderName, pdfBytes);
        }
        else
        {
            // HTML içeriği body'den veya ContentFileUrl'den al
            var htmlContent = mail.Body;

            // Body boş ama ContentFileUrl varsa → DigitalOcean Spaces'den çek
            if (string.IsNullOrEmpty(htmlContent) && !string.IsNullOrEmpty(mail.ContentFileUrl))
            {
                _logger.LogInformation(
                    "ContentFileUrl'den HTML çekiliyor: MailId={Id}, Url={Url}",
                    mail.Id, mail.ContentFileUrl);

                using var http = new HttpClient();
                http.Timeout = TimeSpan.FromSeconds(15);
                htmlContent = await http.GetStringAsync(mail.ContentFileUrl);
            }

            if (!string.IsNullOrEmpty(htmlContent))
            {
                _logger.LogInformation(
                    "HTML body parse ediliyor: MailId={Id}, Provider={Provider}",
                    mail.Id, mail.ProviderName);

                parsed = _pdfParser.ParseHtml(mail.ProviderName, htmlContent);

                // Hâlâ null ve body'de tablo var → SUENO parser'ı zorla dene
                if (parsed is null && htmlContent.Contains("<table", StringComparison.OrdinalIgnoreCase))
                    parsed = _pdfParser.ParseHtml("SUENO", htmlContent);
            }
        }

        if (parsed is null)
        {
            _logger.LogWarning("Parse başarısız: MailId={Id}", mail.Id);
            mail.Status = "FAILED";
            mail.ConvertErrorMessage = $"Mail body parse edilemedi. Provider: {mail.ProviderName}";
            await context.SaveChangesAsync();
            return;
        }

        // 3. Rezervasyon oluştur
        var reservation = new Reservation
        {
            // Mail'den gelen ReservationType öncelikli (subject'ten tespit edildi)
            // Parser da bir şey döndürdüyse parser'ı kullan, ikisi de boşsa NEW
            ReservationType = !string.IsNullOrEmpty(mail.ReservationType) && mail.ReservationType != "NEW"
                              ? mail.ReservationType
                              : (!string.IsNullOrEmpty(parsed.ReservationType) ? parsed.ReservationType : "NEW"),
            ProviderName = mail.ProviderName,
            Voucher      = parsed.Voucher,
            GuestName    = string.IsNullOrEmpty(parsed.GuestName)
                           ? "Unknown Guest" : parsed.GuestName,
            RoomType     = string.IsNullOrEmpty(parsed.RoomType)
                           ? "STANDARD" : parsed.RoomType,
            Pension      = parsed.Pension,
            CheckIn      = parsed.CheckIn == default
                           ? DateOnly.FromDateTime(DateTime.Today) : parsed.CheckIn,
            CheckOut     = parsed.CheckOut == default
                           ? DateOnly.FromDateTime(DateTime.Today.AddDays(7)) : parsed.CheckOut,
            AdultCount   = parsed.AdultCount > 0 ? parsed.AdultCount : 2,
            ChildCount   = parsed.ChildCount,
            Source       = mail.ProviderName,
            Status       = "PENDING",
            SaleDate     = DateTime.UtcNow
        };

        context.Reservations.Add(reservation);

        // 4. Mail durumunu güncelle
        mail.Status      = "CONVERTED";
        mail.IsRead      = true;
        mail.ConvertedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        _logger.LogInformation(
            "Rezervasyon oluşturuldu: MailId={Id}, ReservationId={ResId}, Guest={Guest}, Voucher={Voucher}",
            mail.Id, reservation.Id, reservation.GuestName, reservation.Voucher);
    }
}
