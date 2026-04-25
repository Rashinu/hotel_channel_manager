namespace HotelChannelManager.Services;

// MailKit  → .NET için IMAP/SMTP kütüphanesi. Microsoft'un System.Net.Mail'inden çok daha güçlü.
// MimeKit  → Mail formatı (MIME) parser'ı. MailKit'in içinde kullanılır.
using MailKit;           // FolderAccess, MessageFlags gibi temel tipler
using MailKit.Net.Imap;  // ImapClient — sunucuya bağlanan ana sınıf
using MailKit.Search;    // SearchQuery — sunucu tarafında arama sorguları
using MailKit.Security;  // SecureSocketOptions — SSL/TLS seçenekleri
using HotelChannelManager.Models;

public class ImapMailFetchService
{
    // ILogger: uygulama çalışırken terminale / log dosyasına bilgi yazar.
    // "Kaç mail bulundu, kim gönderdi, ne zaman bağlandı" gibi şeyleri izlemek için.
    // Dependency Injection ile dışarıdan gelir — sen new'lemezsin.
    private readonly ILogger<ImapMailFetchService> _logger;

    public ImapMailFetchService(ILogger<ImapMailFetchService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// IMAP ile mail sunucusuna bağlanır, okunmamış mailleri çeker.
    /// Her mail bir IncomingMail nesnesine dönüştürülür ve liste olarak döner.
    /// Çekilen mailler sunucuda "okundu" işaretlenir — bir daha çekilmez.
    /// </summary>
    public async Task<List<IncomingMail>> FetchUnreadAsync(
        string imapHost,   // Sunucu adresi:  "imap-mail.outlook.com"
        int imapPort,      // Port numarası:   993 (SSL için standart IMAP portu)
        string email,      // Hesap adresi:   "murat_keskin_2014@hotmail.com"
        string password)   // Şifre/App Pass: "xxxx xxxx xxxx xxxx"
    {
        // Döneceğimiz liste. Her başarılı mail buraya eklenir.
        var result = new List<IncomingMail>();

        // ImapClient: sunucuyla konuşan nesne.
        // "using var" → metod bitince (hata olsa bile) bağlantı otomatik kapatılır.
        // Elle Dispose() çağırmak zorunda kalmayız — bağlantı sızıntısı olmaz.
        using var client = new ImapClient();

        // ADIM 1 — BAĞLAN (SSL/TLS)
        // SslOnConnect: bağlanır bağlanmaz SSL el sıkışması başlar.
        // Tüm trafik şifreli tünelden geçer. Şifren düz metin gitmez.
        // Not: port 587 olsaydı StartTls kullanırdık (önce bağlan, sonra şifrele).
        _logger.LogInformation("IMAP bağlanıyor: {Host}:{Port} → {Email}", imapHost, imapPort, email);
        await client.ConnectAsync(imapHost, imapPort, SecureSocketOptions.SslOnConnect);

        // ADIM 2 — KİMLİK DOĞRULA
        // Sunucuya "Ben buyum, şifrem bu" der.
        // Hata: Microsoft consumer Hotmail hesaplarında basic auth kapalı →
        //       OAuth2 (Graph API) ile authenticate etmek gerekiyor.
        await client.AuthenticateAsync(email, password);
        _logger.LogInformation("IMAP kimlik doğrulandı: {Email}", email);

        // ADIM 3 — INBOX'I AÇ
        // client.Inbox: her IMAP sunucusunda zorunlu olan "INBOX" klasörü.
        // ReadWrite: hem okuyacağız hem de okundu işareti koyacağız.
        // ReadOnly olsaydı AddFlagsAsync çağıramazdık.
        var inbox = client.Inbox
            ?? throw new InvalidOperationException("INBOX açılamadı");
        await inbox.OpenAsync(FolderAccess.ReadWrite);
        _logger.LogInformation("INBOX açıldı. Toplam mesaj: {Count}", inbox.Count);

        // ADIM 4 — OKUNMAMIŞ MAİLLERİ BUL
        // SearchQuery.NotSeen: sunucuya "\Seen flag'i olmayan mesajları ver" der.
        // ÖNEMLİ: Bu satır mailleri İNDİRMEZ. Sadece UID listesi döner: [3, 7, 12]
        // 10.000 mail olsa bile sadece ID listesi gelir — hızlı ve hafif.
        var unreadUids = (await inbox.SearchAsync(SearchQuery.NotSeen)) ?? [];
        _logger.LogInformation("Okunmamış mail sayısı: {Count}", unreadUids.Count);

        foreach (var uid in unreadUids)
        {
            // ADIM 5 — MAİLİ İNDİR
            // Asıl indirme burada olur. UID'yi veriyoruz, sunucu o mailin
            // tamamını gönderiyor: header'lar, body, ekler. MimeMessage = tam mail.
            var message = await inbox.GetMessageAsync(uid);

            // ADIM 6 — GÖNDERENİ AL
            // message.From → liste (teorik olarak birden fazla gönderen olabilir).
            // FirstOrDefault() → ilkini al, liste boşsa null döner.
            // ?.Address → null-safe erişim, null gelirse hata vermez.
            // ?? string.Empty → null gelirse boş string yaz, DB'ye null gitmesin.
            var fromAddress = message.From.Mailboxes.FirstOrDefault()?.Address ?? string.Empty;

            // ADIM 7 — ALICILARI AL
            // message.To → alıcılar listesi. Birden fazla olabilir.
            // Select(m => m.Address) → her alıcının sadece mail adresini al.
            // string.Join → hepsini virgülle birleştir: "res@hotel.com, cc@hotel.com"
            var toAddress = string.Join(", ", message.To.Mailboxes.Select(m => m.Address));

            // ADIM 8 — BODY'Yİ AL (ÖNCELİK SIRASI ÖNEMLİ)
            // ?? (null-coalescing): soldan sağa dener, ilk null-olmayan değeri alır.
            // 1. HtmlBody → HTML versiyonu varsa al. Tablolar, renkler korunur.
            //               SuenoTur parser HTML tablosuna bakıyor, bu yüzden önce HTML.
            // 2. TextBody → HTML yoksa düz metin al. Tablo formatı kaybolur ama bir şeyler var.
            // 3. string.Empty → ikisi de yoksa boş string. DB'ye null gitmesin.
            var body = message.HtmlBody ?? message.TextBody ?? string.Empty;

            // ADIM 9 — EK VAR MI?
            // message.Attachments → maile ekli dosyalar (PDF, Excel vb.)
            // .Any() → en az bir ek varsa true döner.
            // OTS/MTS → PDF ek gönderir (HasAttachments=true), PDF parser devreye girer.
            // SuenoTur → body içinde tablo yazar, ek göndermez (HasAttachments=false).
            var hasAttachments = message.Attachments.Any();

            // ADIM 10 — PROVIDER'I TESPİT ET
            // From adresinden hangi acente olduğunu tahmin et.
            // Bu bilgi sonra MailIntegrationService'de hangi parser çalışacağını belirler.
            var providerName = DetectProvider(fromAddress, message.Subject);

            // ADIM 11 — IncomingMail NESNESİ OLUŞTUR
            // Mailin zarfı (from, to, subject, date) ve ham içeriği (body) DB modeline dönüştürülür.
            // Body bu aşamada ham HTML olarak saklanır — parser sonra açacak.
            var incoming = new IncomingMail
            {
                // IMAP'in uid numarası. "Bu maili daha önce çektim mi?" kontrolünde kullanılır.
                // MailController'da: AnyAsync(m => m.UniqueId == mail.UniqueId)
                UniqueId = uid.ToString(),

                // Mailin kendi evrensel ID'si. Outlook detayında görünen uzun string (AAMkAGRl...).
                // Fallback: MessageId yoksa IMAP uid'ini kullan.
                UidKey = message.MessageId ?? uid.ToString(),

                // Gönderenin kendi mail client'ında yazan tarih.
                // UtcDateTime: zaman dilimi farkını ortadan kaldırır. Her zaman UTC sakla.
                MailDate = message.Date.UtcDateTime,

                // Bizim uygulamanın maili çektiği an. Sunucudan değil, sistemimizden geliyor.
                ReceiveDate = DateTime.UtcNow,

                FromAddress    = fromAddress,                  // "zbalbay@suenotur.com"
                ToAddress      = toAddress,                    // "reservation@hotel.com"
                Subject        = message.Subject ?? string.Empty,
                Body           = body,                         // Ham HTML — parser sonra açacak
                HasAttachments = hasAttachments,
                IsRead         = false,                        // Kullanıcı henüz panelde görmedi

                // "SUENO", "OTS/MTS", "JOLLYTUR" — hangi parser çalışacak?
                ProviderName = providerName,

                // PENDING: henüz rezervasyona çevrilmedi.
                // Convert endpoint çağrılınca "CONVERTED" veya "FAILED" olacak.
                Status = "PENDING"
            };

            result.Add(incoming);

            // ADIM 12 — SUNUCUDA OKUNDU İŞARETLE
            // MessageFlags.Seen → IMAP'in \Seen flag'ini ekler.
            // Bir daha SearchQuery.NotSeen çalışırsa bu mail gelmez.
            // silent: true → sunucuya "bunu bana bildirme, sadece yap" der.
            //                Gereksiz network mesajını keser.
            await inbox.AddFlagsAsync(uid, MessageFlags.Seen, silent: true);

            _logger.LogInformation(
                "Mail alındı: From={From}, Subject={Subject}, Provider={Provider}",
                fromAddress, message.Subject, providerName);
        }

        // Bağlantıyı temiz kapat. "true" → sunucuya LOGOUT komutu gönder.
        await client.DisconnectAsync(true);
        return result;
    }

    /// <summary>
    /// Gönderenin mail adresine ve konu satırına bakarak hangi acente olduğunu tahmin eder.
    /// Şu an domain bazlı basit eşleştirme yapıyor.
    /// İleride: DB'deki ProviderIntegrations tablosundaki SenderDomain kolonu ile eşleştirilecek.
    /// </summary>
    private static string DetectProvider(string fromAddress, string? subject)
    {
        // ToLowerInvariant: "SuenoTur@..." veya "SUENOTUR@..." hepsini yakalar.
        // Invariant: Türkçe I/İ sorunu yok. Kültür bağımsız küçük harf.
        var from = fromAddress.ToLowerInvariant();
        var sub  = (subject ?? string.Empty).ToLowerInvariant();

        // Domain kontrolü — soldan sağa, ilk eşleşmede döner.
        if (from.Contains("suenotur") || from.Contains("sueno"))  return "SUENO";
        if (from.Contains("mtsglobe") || from.Contains("ots"))    return "OTS/MTS";
        if (from.Contains("jollytur"))                             return "JOLLYTUR";
        if (from.Contains("tatilsepeti"))                          return "TATILSEPETI";
        if (from.Contains("booking.com"))                          return "BOOKING";
        if (from.Contains("expedia"))                              return "EXPEDIA";

        // Domain'den anlaşılamadıysa konu satırına bak.
        if (sub.Contains("voucher") || sub.Contains("rezervasyon")) return "UNKNOWN_RESERVATION";

        // Hiçbiri eşleşmediyse: panelde görünür, el ile işlenir.
        return "UNKNOWN";
    }
}