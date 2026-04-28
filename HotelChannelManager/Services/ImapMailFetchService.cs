namespace HotelChannelManager.Services;

using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using HotelChannelManager.Models;

public class ImapMailFetchService(ILogger<ImapMailFetchService> _logger)
{

    // IMAP ile bağlanır, okunmamış mailleri çeker, DB modeline dönüştürür.
    // Çekilen mailler sunucuda okundu olarak işaretlenir — tekrar çekilmez.
    public async Task<List<IncomingMail>> FetchUnreadAsync(
        string imapHost, int imapPort, string email, string password)
    {
        var result = new List<IncomingMail>();

        using var client = new ImapClient();

        _logger.LogInformation("IMAP bağlanıyor: {Host}:{Port}", imapHost, imapPort);
        await client.ConnectAsync(imapHost, imapPort, SecureSocketOptions.SslOnConnect);
        await client.AuthenticateAsync(email, password);

        var inbox = client.Inbox
            ?? throw new InvalidOperationException("INBOX açılamadı");
        await inbox.OpenAsync(FolderAccess.ReadWrite);

        // Sadece UID listesi gelir, mailler henüz indirilmez
        var unreadUids = (await inbox.SearchAsync(SearchQuery.NotSeen)) ?? [];
        _logger.LogInformation("Okunmamış mail: {Count}", unreadUids.Count);

        foreach (var uid in unreadUids)
        {
            // Maili tam indir (header + body + ekler)
            var message = await inbox.GetMessageAsync(uid);

            var fromAddress = message.From.Mailboxes.FirstOrDefault()?.Address ?? string.Empty;
            var toAddress   = string.Join(", ", message.To.Mailboxes.Select(m => m.Address));

            // Önce HTML body, yoksa plain text
            var body = message.HtmlBody ?? message.TextBody ?? string.Empty;

            var incoming = new IncomingMail
            {
                UniqueId        = uid.ToString(),
                UidKey          = message.MessageId ?? uid.ToString(),
                MailDate        = message.Date.UtcDateTime,
                ReceiveDate     = DateTime.UtcNow,
                FromAddress     = fromAddress,
                ToAddress       = toAddress,
                Subject         = message.Subject ?? string.Empty,
                Body            = body,
                ContentFileUrl  = ExtractContentFileUrl(body),
                HasAttachments  = message.Attachments.Any(),
                IsRead          = false,
                ProviderName    = DetectProvider(fromAddress, message.Subject),
                ReservationType = DetectReservationType(message.Subject),
                Status          = "PENDING"
            };

            result.Add(incoming);

            // Sunucuda okundu işaretle — bir daha çekilmez
            await inbox.AddFlagsAsync(uid, MessageFlags.Seen, silent: true);

            _logger.LogInformation("Mail alındı: {From} | {Subject}", fromAddress, message.Subject);
        }

        await client.DisconnectAsync(true);
        return result;
    }

    // From adresine göre hangi acente olduğunu tahmin eder
    private static string DetectProvider(string fromAddress, string? subject)
    {
        var from = fromAddress.ToLowerInvariant();
        var sub  = (subject ?? string.Empty).ToLowerInvariant();

        if (from.Contains("suenotur") || from.Contains("sueno"))  return "SUENO";
        if (from.Contains("mtsglobe") || from.Contains("ots"))    return "OTS/MTS";
        if (from.Contains("jollytur"))                             return "JOLLYTUR";
        if (from.Contains("tatilsepeti"))                          return "TATILSEPETI";
        if (from.Contains("booking.com"))                          return "BOOKING";
        if (sub.Contains("voucher") || sub.Contains("rezervasyon")) return "UNKNOWN_RESERVATION";

        return "UNKNOWN";
    }

    // Subject'e göre rezervasyon tipini belirler
    // "IPTAL" → CANCELLED, "DEGISIKLIK" → CHANGED, diğerleri → NEW
    private static string DetectReservationType(string? subject)
    {
        var sub = (subject ?? string.Empty).ToUpperInvariant();

        if (sub.Contains("IPTAL") || sub.Contains("CANCEL"))           return "CANCELLED";
        if (sub.Contains("DEGISIKLIK") || sub.Contains("DEĞİŞİKLİK") ||
            sub.Contains("CHANGE")     || sub.Contains("BILGISI"))     return "CHANGED";

        return "NEW";
    }

    // Body'deki DigitalOcean Spaces URL'sini çıkarır
    // Gerçek SUENO TUR mailleri içeriği body'de değil bu URL'de tutar
    private static string? ExtractContentFileUrl(string body)
    {
        if (string.IsNullOrEmpty(body)) return null;

        var match = System.Text.RegularExpressions.Regex.Match(
            body,
            @"https?://[^\s""'<>]+digitaloceanspaces\.com[^\s""'<>]*",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return match.Success ? match.Value : null;
    }
}