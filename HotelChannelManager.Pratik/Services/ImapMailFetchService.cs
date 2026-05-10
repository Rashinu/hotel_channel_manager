namespace HotelChannelManager.Pratik.Services;

using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using HotelChannelManager.Pratik.Models;

// Giriş : imapHost, imapPort, email, password
// Çıkış : List<IncomingMail>

public class ImapMailFetchService
{
    public async Task<List<IncomingMail>> FetchUnreadAsync(
        string imapHost, int imapPort, string email, string password)
    {
        var result = new List<IncomingMail>();

        using var client = new ImapClient();

        await client.ConnectAsync(imapHost, imapPort, SecureSocketOptions.SslOnConnect);
        await client.AuthenticateAsync(email, password);

        var inbox = client.Inbox
            ?? throw new InvalidOperationException("INBOX açılamadı");
        await inbox.OpenAsync(FolderAccess.ReadWrite);

        var unreadUids = (await inbox.SearchAsync(SearchQuery.NotSeen)) ?? [];

        foreach (var uid in unreadUids)
        {
            var message = await inbox.GetMessageAsync(uid);

            var fromAddress = message.From.Mailboxes.FirstOrDefault()?.Address ?? string.Empty;
            var toAddress   = string.Join(", ", message.To.Mailboxes.Select(m => m.Address));
            var body        = message.HtmlBody ?? message.TextBody ?? string.Empty;

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

            await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true);
        }

        await client.DisconnectAsync(true);
        return result;
    }

    private static string DetectProvider(string fromAddress, string? subject)
    {
        var from = fromAddress.ToLowerInvariant();
        var sub  = subject?.ToLowerInvariant() ?? string.Empty;

        if (from.Contains("sueno") || sub.Contains("sueno"))       return "SUENO";
        if (from.Contains("mtsglobe") || from.Contains("ots"))     return "OTS/MTS";
        if (from.Contains("jollytur") || sub.Contains("jollytur")) return "JOLLYTUR";

        return "UNKNOWN";
    }

    private static string DetectReservationType(string? subject)
    {
        var sub = (subject ?? string.Empty).ToUpperInvariant();

        if (sub.Contains("IPTAL") || sub.Contains("CANCEL"))           return "CANCELLED";
        if (sub.Contains("DEGISIKLIK") || sub.Contains("DEĞİŞİKLİK") ||
            sub.Contains("CHANGE")     || sub.Contains("BILGISI"))     return "CHANGED";

        return "NEW";
    }

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
