namespace HotelChannelManager.Services;

using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

public class SmtpSenderService
{
    private readonly ILogger<SmtpSenderService> _logger;

    public SmtpSenderService(ILogger<SmtpSenderService> logger)
    {
        _logger = logger;
    }

    public async Task SendAsync(
        string smtpHost,
        int smtpPort,
        string senderEmail,
        string senderPassword,
        string toEmail,
        string subject,
        string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Test Sender", senderEmail));
        message.To.Add(new MailboxAddress("Receiver", toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

        _logger.LogInformation(
            "SMTP bağlanıyor: {Host}:{Port} → {To}",
            smtpHost, smtpPort, toEmail);

        await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(senderEmail, senderPassword);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation("Mail gönderildi: {Subject} → {To}", subject, toEmail);
    }
}
