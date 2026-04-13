namespace HotelChannelManager.Services;

using HotelChannelManager.Data;
using HotelChannelManager.Models;
using Microsoft.EntityFrameworkCore;

public class MailIntegrationService
{
    private readonly ILogger<MailIntegrationService> _logger;

    public MailIntegrationService(ILogger<MailIntegrationService> logger)
    {
        _logger = logger;
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

        _logger.LogInformation(
            "Entegrasyon bulundu: Provider={Provider}, VoucherType={VoucherType}",
            integration.ProviderName, integration.VoucherType);

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

        // 3. Faz 8'de PDF parse edilecek
        // Şimdilik kuyruğa aldık de
        _logger.LogInformation(
            "PDF kuyruğa alındı: MailId={Id}, Attachment={Attachment}",
            mail.Id, mail.AttachmentName);

        mail.Status = "CONVERTED";
        mail.ConvertedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        _logger.LogInformation(
            "Mail başarıyla işlendi: MailId={Id}", mail.Id);
    }
}