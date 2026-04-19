namespace HotelChannelManager.Services;

using HotelChannelManager.Data;
using HotelChannelManager.Models;
using Microsoft.EntityFrameworkCore;

public class MailIntegrationService
{
    private readonly ILogger<MailIntegrationService> _logger;
    private readonly PdfParserService _pdfParser;

    public MailIntegrationService(
        ILogger<MailIntegrationService> logger,
        PdfParserService pdfParser)
    {
        _logger = logger;
        _pdfParser = pdfParser;
    }

    public async Task ProcessMailAsync(
        IncomingMail mail,
        AppDbContext context)
    {
        _logger.LogInformation(
            "Mail işleniyor: From={From}, Provider={Provider}",
            mail.From, mail.ProviderName);

        // 1. Entegrasyon var mı kontrol et
        var integration = await context.ProviderIntegrations
            .FirstOrDefaultAsync(p =>
                p.ProviderName == mail.ProviderName &&
                p.IsActive);

        if (integration is null)
        {
            _logger.LogWarning(
                "Entegrasyon bulunamadı: Provider={Provider}",
                mail.ProviderName);

            mail.Status = "FAILED";
            mail.ErrorMessage = $"Entegrasyon bulunamadı: {mail.ProviderName}";
            await context.SaveChangesAsync();
            return;
        }

        // 2. PDF eki var mı?
        if (string.IsNullOrEmpty(mail.AttachmentName))
        {
            _logger.LogWarning(
                "PDF eki yok: MailId={Id}", mail.Id);

            mail.Status = "FAILED";
            mail.ErrorMessage = "PDF eki bulunamadı";
            await context.SaveChangesAsync();
            return;
        }

        // 3. PDF parse et
        _logger.LogInformation(
            "PDF parse ediliyor: MailId={Id}, Provider={Provider}",
            mail.Id, mail.ProviderName);

        // Fake PDF content → byte dizisine çevir
        var pdfBytes = System.Text.Encoding.UTF8
            .GetBytes(mail.AttachmentContent ?? string.Empty);

        var parsed = _pdfParser.Parse(mail.ProviderName, pdfBytes);

        if (parsed is null)
        {
            _logger.LogWarning(
                "PDF parse başarısız: MailId={Id}", mail.Id);

            mail.Status = "FAILED";
            mail.ErrorMessage = "PDF parse edilemedi";
            await context.SaveChangesAsync();
            return;
        }

        // 4. Rezervasyon oluştur
        var reservation = new Reservation
        {
            GuestName = string.IsNullOrEmpty(parsed.GuestName)
                ? "Unknown Guest" : parsed.GuestName,
            RoomType = string.IsNullOrEmpty(parsed.RoomType)
                ? "STANDARD" : parsed.RoomType,
            CheckIn = parsed.CheckIn == default
                ? DateOnly.FromDateTime(DateTime.Today) : parsed.CheckIn,
            CheckOut = parsed.CheckOut == default
                ? DateOnly.FromDateTime(DateTime.Today.AddDays(7)) : parsed.CheckOut,
            Source = mail.ProviderName,
            Status = "PENDING"
        };

        context.Reservations.Add(reservation);

        // 5. Mail durumunu güncelle
        mail.Status = "CONVERTED";
        mail.ConvertedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        _logger.LogInformation(
            "Rezervasyon oluşturuldu: MailId={Id}, ReservationId={ResId}, Guest={Guest}",
            mail.Id, reservation.Id, reservation.GuestName);
    }
}