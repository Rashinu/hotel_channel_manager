namespace HotelChannelManager.Services.Parsers;

// HtmlAgilityPack → HTML string'i DOM ağacına çeviren kütüphane.
//                   Browser'ın yaptığını kod tarafında yapıyor.
//                   XPath sorgularıyla istediğin elementi seçebiliyorsun.
using HotelChannelManager.Services;
using HtmlAgilityPack;
using System.Globalization; // CultureInfo.InvariantCulture — tarih parse için

// Bu dosyanın tek görevi:
// SUENO TUR'un gönderdiği HTML mail body'sini alıp içindeki
// rezervasyon tablosundan verileri çıkarmak ve ParsedReservation nesnesine doldurmak.
//
// Giriş:  <html>...<table><th>VOUCHER KODU</th>...<td>600701</td>...</table>...</html>
// Çıkış:  ParsedReservation { Voucher="600701", GuestName="ERTUGRUL SIPAHI", ... }

public class SuenoTurParser
{
    // SUENO TUR mail tablosunun kolon sırası (soldan sağa):
    // VOUCHER KODU | YOLCU ADI SOYADI | P.D. | ODA | ACIKLAMA | KATEGORİ |
    // DATEİN | DATEOUT | ULASIM | BURO | SATICI | İNDİRİM | TUTAR | KAYIT GUNU

    public ParsedReservation? Parse(string html)
    {
        // Herhangi bir hata olursa null dön — MailIntegrationService "FAILED" yazar.
        // Exception fırlatmak yerine null dönmek tercih edilir çünkü:
        // parse hatası bir sistem hatası değil, beklenen bir durumdur.
        try
        {
            // ADIM 1 — HTML'İ DOM AĞACINA ÇEVİR
            // HtmlDocument: tarayıcının HTML'i nasıl yorumladığını taklit eder.
            // LoadHtml() sonrası doc.DocumentNode üzerinden XPath sorgularıyla
            // istediğin HTML elementine ulaşabilirsin.
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // ADIM 2 — TÜM TABLO SATIRLARINI SEÇ
            // XPath: "//table//tr" → sayfadaki herhangi bir tablonun herhangi bir <tr>'si
            // "//" → derinlik fark etmez, her seviyede ara
            // "table//tr" → table içindeki tr'ler (thead/tbody/tfoot fark etmez)
            var rows = doc.DocumentNode.SelectNodes("//table//tr");

            // Hiç satır yoksa veya sadece 1 satır varsa (sadece header, veri yok) → null dön.
            if (rows == null || rows.Count < 2) return null;

            // ADIM 3 — HEADER SATIRINI BUL
            // Sorun: tabloda kaçıncı satırın header olduğunu bilmiyoruz.
            // Çözüm: "VOUCHER" veya "DATEİN" kelimesi geçen satırı header say.
            // Bu sayede tablonun üstünde başka satırlar olsa bile doğru satırı buluruz.
            int headerIndex = -1;  // -1 = henüz bulunamadı
            List<string> headers = new();

            for (int i = 0; i < rows.Count; i++)
            {
                // Her satırdaki hücreleri al. "th|td" → hem <th> hem <td> seç.
                // (SUENO TUR header'ı <th> kullanıyor ama bazı mailler <td> kullanır)
                var cells = rows[i].SelectNodes("th|td");
                if (cells == null) continue; // Boş satır, atla

                // Her hücrenin metnini temizleyerek listeye al.
                // CleanText: HTML entity'leri decode eder (&amp; → &), boşlukları düzeltir.
                var texts = cells.Select(c => CleanText(c.InnerText)).ToList();

                // Bu satırda "VOUCHER" veya "DATEİN" geçiyor mu?
                // .Any() → listede en az bir eleman koşulu sağlıyorsa true döner.
                if (texts.Any(t => t.Contains("VOUCHER") || t.Contains("DATEIN") || t.Contains("DATEİN")))
                {
                    headerIndex = i;  // Bu satırın indeksini kaydet
                    headers = texts;  // Header metinlerini kaydet
                    break;            // Bulduk, döngüden çık
                }
            }

            // Header bulunamadıysa veya header'dan sonra veri satırı yoksa → null dön.
            // headerIndex + 1 >= rows.Count: header son satırsa veri satırı yoktur.
            if (headerIndex < 0 || headerIndex + 1 >= rows.Count) return null;

            // ADIM 4 — KOLON İNDEKSLERİNİ ÇIKAR
            // Header: ["VOUCHER KODU", "YOLCU ADI SOYADI", "P.D.", "ODA", "ACIKLAMA", "KATEGORİ", ...]
            // IndexOf("VOUCHER") → 0   (0. kolonda bulundu)
            // IndexOf("YOLCU")   → 1   (1. kolonda bulundu)
            // Bu indeksler sonraki adımda veri satırından doğru hücreyi almak için kullanılır.
            int idxVoucher   = IndexOf(headers, "VOUCHER");
            int idxGuest     = IndexOf(headers, "YOLCU");
            int idxPension   = IndexOf(headers, "P.D.");     // Pansiyon: AI, BB, HB...
            int idxRoom      = IndexOf(headers, "ODA");      // Oda tipi: DOUBLE +1 CHD
            int idxCategory  = IndexOf(headers, "KATEG");    // KATEGORİ: DELUXE DENIZ STD
            int idxDateIn    = IndexOf(headers, "DATEIN", "DATEİN"); // Giriş tarihi
            int idxDateOut   = IndexOf(headers, "DATEOUT");  // Çıkış tarihi
            int idxAgency    = IndexOf(headers, "BURO");     // Büro/Acente
            int idxSeller    = IndexOf(headers, "SATICI");   // Satıcı
            int idxTotal     = IndexOf(headers, "TUTAR");    // Toplam tutar
            int idxTransport = IndexOf(headers, "ULASIM");   // Ulaşım: KENDI, UCAK...

            // ADIM 5 — VERİ SATIRINI AL
            // Header'dan hemen sonraki satır (headerIndex + 1) asıl rezervasyon verisi.
            var dataRow = rows[headerIndex + 1].SelectNodes("th|td");
            if (dataRow == null) return null;

            // Veri satırının hücrelerini de temizleyerek listeye al.
            // data: ["600701", "ERTUGRUL SIPAHI", "AI", "DOUBLE +1 CHD", "", "DELUXE DENIZ STD", ...]
            var data = dataRow.Select(c => CleanText(c.InnerText)).ToList();

            // ADIM 6 — ParsedReservation NESNESİNİ DOLDUR
            // GetCell(data, idx): data listesinden idx numaralı elemanı güvenli çeker.
            // idx = -1 ise (kolon bulunamadıysa) boş string döner, exception vermez.
            var reservation = new ParsedReservation
            {
                ProviderName    = "SUENO",
                ReservationType = "NEW",
                Voucher         = GetCell(data, idxVoucher),   // "600701"
                GuestName       = GetCell(data, idxGuest),     // "ERTUGRUL SIPAHI"
                Pension         = GetCell(data, idxPension),   // "AI"

                // KATEGORİ varsa onu al (daha spesifik: "DELUXE DENIZ STD"),
                // yoksa ODA kolonunu al ("DOUBLE +1 CHD").
                // string.IsNullOrEmpty: boş string de null gibi davranıyor.
                RoomType = string.IsNullOrEmpty(GetCell(data, idxCategory))
                           ? GetCell(data, idxRoom)
                           : GetCell(data, idxCategory),

                CheckIn  = ParseDate(GetCell(data, idxDateIn)),   // "28/08/2026" → DateOnly
                CheckOut = ParseDate(GetCell(data, idxDateOut)),  // "03/09/2026" → DateOnly
            };

            return reservation;
        }
        catch
        {
            // Beklenmedik bir hata oldu (bozuk HTML, tamamen farklı format vb.)
            // null dönüyoruz — üst katman "FAILED" yazacak.
            return null;
        }
    }

    // YARDIMCI METOD 1 — KOLON İNDEKSİ BUL
    // headers listesinde arama kelimelerinden herhangi biri geçen ilk elemanın indeksini döner.
    // params string[] searches → birden fazla alternatif aranabilir:
    //   IndexOf(headers, "DATEIN", "DATEİN") → "DATEİN" veya "DATEIN" olan kolonu bul
    // Bulunamazsa -1 döner → GetCell -1'i görünce boş string döner.
    private static int IndexOf(List<string> headers, params string[] searches)
    {
        for (int i = 0; i < headers.Count; i++)
        {
            // ToUpperInvariant: hem header hem arama kelimesini büyük harfe çevir.
            // "KATEGORİ".Contains("kateg") yerine "KATEGORİ".Contains("KATEG") — daha güvenli.
            var h = headers[i].ToUpperInvariant();
            if (searches.Any(s => h.Contains(s.ToUpperInvariant())))
                return i;
        }
        return -1; // Bulunamadı
    }

    // YARDIMCI METOD 2 — HÜCREDEN VERİ ÇIKAR
    // data listesinden idx numaralı elemanı güvenli çeker.
    // idx < 0: kolon bulunamadıydı (IndexOf -1 döndürdü) → boş string dön.
    // idx >= data.Count: veri satırı header'dan daha kısa → boş string dön.
    private static string GetCell(List<string> data, int idx)
    {
        if (idx < 0 || idx >= data.Count) return string.Empty;
        return data[idx];
    }

    // YARDIMCI METOD 3 — HTML METNİNİ TEMİZLE
    // Ham HTML hücre içeriği: "&nbsp;ERTUGRUL\n  SIPAHI&nbsp;"
    // Temizlenmiş: "ERTUGRUL SIPAHI"
    private static string CleanText(string raw)
    {
        // HtmlDecode: HTML entity'leri gerçek karaktere çevirir.
        // "&amp;" → "&", "&nbsp;" → " ", "&lt;" → "<"
        var decoded = System.Net.WebUtility.HtmlDecode(raw ?? string.Empty);

        // Regex \s+ → bir veya daha fazla boşluk/tab/newline karakterini tek boşlukla değiştir.
        // .Trim() → başta ve sonda kalan boşlukları at.
        return System.Text.RegularExpressions.Regex
            .Replace(decoded, @"\s+", " ").Trim();
    }

    // YARDIMCI METOD 4 — TARİH STRİNG'İNİ DateOnly'e ÇEVİR
    // SUENO TUR tarihleri "28/08/2026" formatında gelir.
    // Ama farklı sistemler farklı format gönderebilir — hepsini deniyoruz.
    private static DateOnly ParseDate(string raw)
    {
        // Boş string gelirse bugünü döndür (fallback).
        if (string.IsNullOrEmpty(raw)) return DateOnly.FromDateTime(DateTime.Today);

        // Deneyeceğimiz format listesi — sırayla denenir, ilk başarılı olan kullanılır.
        // "dd" → 2 haneli gün (28), "d" → 1 veya 2 haneli gün (7 veya 28)
        // "MM" → 2 haneli ay (08), "M" → 1 veya 2 haneli ay (8 veya 12)
        // "yyyy" → 4 haneli yıl, "yy" → 2 haneli yıl (26 → 2026)
        string[] formats =
        {
            "dd/MM/yyyy", "d/M/yyyy",   // 28/08/2026 veya 7/8/2026
            "dd.MM.yyyy", "d.M.yyyy",   // 28.08.2026 veya 7.8.2026
            "yyyy-MM-dd",               // 2026-08-28 (ISO format)
            "dd/MM/yy",   "d/M/yy"     // 28/08/26 veya 7/8/26
        };

        foreach (var fmt in formats)
        {
            // TryParseExact: format tam eşleşmesi arar. Başarısız olursa exception değil false döner.
            // CultureInfo.InvariantCulture: ay/gün sırası kültüre göre değişmesin.
            // DateTimeStyles.None: ekstra tolerans yok, tam format bekleniyor.
            if (DateOnly.TryParseExact(raw.Trim(), fmt,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var d))
                return d; // Başarılı → döndür, diğer formatlara bakma
        }

        // Hiçbir format eşleşmediyse bugünü döndür.
        // Bu durumda log'a bakıp o maili elle düzeltmek gerekir.
        return DateOnly.FromDateTime(DateTime.Today);
    }
}