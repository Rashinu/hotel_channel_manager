namespace HotelChannelManager.Pratik.Services;

using HotelChannelManager.Pratik.Data;
using HotelChannelManager.Pratik.Models;
using HotelChannelManager.Pratik.Services.Parsers;

public class MailIntegrationService
{
    private readonly PdfParserService _pdfParser;
    private readonly AppDbContext _context;

    public MailIntegrationService(PdfParserService pdfParser, AppDbContext context)
    {
        _pdfParser = pdfParser;
        _context   = context;
    }

    public async Task<(bool success, Reservation? reservation)> ProcessMailAsync(IncomingMail mail)
    {
        ParsedReservation? parsed = null;

        if (!string.IsNullOrEmpty(mail.AttachmentContent))
        {
            // PDF eki var → base64 decode → PDF parse
            // CORNER CASE: base64 bozuksa Convert.FromBase64String exception fırlatır → catch ile FAILED olur
            var pdfBytes = Convert.FromBase64String(mail.AttachmentContent);
            parsed = _pdfParser.ParsePdf(mail.ProviderName, pdfBytes);
        }
        else
        {
            var htmlContent = mail.Body;

            if (string.IsNullOrEmpty(htmlContent) && !string.IsNullOrEmpty(mail.ContentFileUrl))
            {
                // CORNER CASE: URL erişilemezse veya timeout olursa → exception → mail FAILED olur
                using var http = new HttpClient();
                http.Timeout = TimeSpan.FromSeconds(15);
                htmlContent = await http.GetStringAsync(mail.ContentFileUrl);
            }

            if (!string.IsNullOrEmpty(htmlContent))
            {
                parsed = _pdfParser.ParseHtml(mail.ProviderName, htmlContent);

                // CORNER CASE: Provider "UNKNOWN" ama içerikte tablo var → SUENO parser'ı dene
                if (parsed is null && htmlContent.Contains("<table", StringComparison.OrdinalIgnoreCase))
                    parsed = _pdfParser.ParseHtml("SUENO", htmlContent);
            }
        }

        if (parsed is null)
        {
            mail.Status = "FAILED";
            mail.ConvertErrorMessage = $"Parse edilemedi. Provider: {mail.ProviderName}";
            await _context.SaveChangesAsync();
            return (false, null);
        }

        // CORNER CASE: Subject "CANCELLED"/"CHANGED" diyorsa parse sonucuna değil subject'e güven
        var reservationType = mail.ReservationType != "NEW" && !string.IsNullOrEmpty(mail.ReservationType)
            ? mail.ReservationType
            : (parsed.ReservationType ?? "NEW");

        var reservation = new Reservation
        {
            ReservationType = reservationType,
            ProviderName    = mail.ProviderName,
            Voucher         = parsed.Voucher,
            GuestName       = string.IsNullOrEmpty(parsed.GuestName) ? "Unknown Guest" : parsed.GuestName,
            RoomType        = string.IsNullOrEmpty(parsed.RoomType)  ? "STANDARD"      : parsed.RoomType,
            Pension         = parsed.Pension,
            // CORNER CASE: Parse başarılı ama tarih okunamadıysa bugün/+7 gün default atanır
            CheckIn         = parsed.CheckIn  == default ? DateOnly.FromDateTime(DateTime.Today)            : parsed.CheckIn,
            CheckOut        = parsed.CheckOut == default ? DateOnly.FromDateTime(DateTime.Today.AddDays(7)) : parsed.CheckOut,
            AdultCount      = parsed.AdultCount > 0 ? parsed.AdultCount : 2,
            ChildCount      = parsed.ChildCount,
            Source          = mail.ProviderName,
            Status          = "PENDING",
            SaleDate        = DateTime.UtcNow
        };

        _context.Reservations.Add(reservation);

        mail.Status      = "CONVERTED";
        mail.IsRead      = true;
        mail.ConvertedAt = DateTime.UtcNow;

        // CORNER CASE: DB bağlantısı koparsa SaveChangesAsync exception fırlatır
        // → reservation kaydedilmez ama mail FAILED olmaz çünkü buraya kadar geldi
        // → Çözüm: transaction kullanılmalı (ikisi birlikte commit ya da rollback)
        await _context.SaveChangesAsync();

        return (true, reservation);
    }
}
