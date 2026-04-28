namespace HotelChannelManager.Services.Parsers;

using HotelChannelManager.Services;
using HtmlAgilityPack;
using System.Globalization;

// SUENO TUR mailindeki HTML tablodan rezervasyon verilerini çıkarır.
// Giriş: <html>...<table><th>VOUCHER KODU</th>...<td>600701</td>...</html>
// Çıkış: ParsedReservation { Voucher, GuestName, CheckIn, CheckOut... }
public class SuenoTurParser
{
    public ParsedReservation? Parse(string html)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Tablodaki tüm satırları al
            var rows = doc.DocumentNode.SelectNodes("//table//tr");
            if (rows == null || rows.Count < 2) return null;

            // "VOUCHER KODU" geçen satır header'dır
            int headerIndex = -1;
            List<string> headers = new();

            for (int i = 0; i < rows.Count; i++)
            {
                var cells = rows[i].SelectNodes("th|td");
                if (cells == null) continue;

                var texts = cells.Select(c => CleanText(c.InnerText)).ToList();

                if (texts.Any(t => t.Contains("VOUCHER") || t.Contains("DATEIN") || t.Contains("DATEİN")))
                {
                    headerIndex = i;
                    headers = texts;
                    break;
                }
            }

            // Header bulunamadı veya veri satırı yok
            if (headerIndex < 0 || headerIndex + 1 >= rows.Count) return null;

            // Her kolonun hangi indekste olduğunu bul
            int idxVoucher   = IndexOf(headers, "VOUCHER");
            int idxGuest     = IndexOf(headers, "YOLCU");
            int idxPension   = IndexOf(headers, "P.D.");
            int idxRoom      = IndexOf(headers, "ODA");
            int idxCategory  = IndexOf(headers, "KATEG");
            int idxDateIn    = IndexOf(headers, "DATEIN", "DATEİN");
            int idxDateOut   = IndexOf(headers, "DATEOUT");

            // Veri satırını al (header'ın hemen altındaki satır)
            var dataRow = rows[headerIndex + 1].SelectNodes("th|td");
            if (dataRow == null) return null;

            var data = dataRow.Select(c => CleanText(c.InnerText)).ToList();

            return new ParsedReservation
            {
                ProviderName    = "SUENO",
                ReservationType = "NEW",
                Voucher         = GetCell(data, idxVoucher),
                GuestName       = GetCell(data, idxGuest),
                Pension         = GetCell(data, idxPension),
                // KATEGORİ varsa onu al (daha spesifik), yoksa ODA kolonunu al
                RoomType = string.IsNullOrEmpty(GetCell(data, idxCategory))
                           ? GetCell(data, idxRoom)
                           : GetCell(data, idxCategory),
                CheckIn  = ParseDate(GetCell(data, idxDateIn)),
                CheckOut = ParseDate(GetCell(data, idxDateOut)),
            };
        }
        catch
        {
            return null;
        }
    }

    // Headers listesinde aranan kelimeyi içeren ilk kolonun indeksini döner, bulamazsa -1
    private static int IndexOf(List<string> headers, params string[] searches)
    {
        for (int i = 0; i < headers.Count; i++)
        {
            var h = headers[i].ToUpperInvariant();
            if (searches.Any(s => h.Contains(s.ToUpperInvariant())))
                return i;
        }
        return -1;
    }

    // İndeks geçersizse boş string döner, geçerliyse o hücreyi döner
    private static string GetCell(List<string> data, int idx)
    {
        if (idx < 0 || idx >= data.Count) return string.Empty;
        return data[idx];
    }

    // HTML entity decode + fazla boşlukları temizler
    private static string CleanText(string raw)
    {
        var decoded = System.Net.WebUtility.HtmlDecode(raw ?? string.Empty);
        return System.Text.RegularExpressions.Regex
            .Replace(decoded, @"\s+", " ").Trim();
    }

    // "28/08/2026", "28.08.2026", "2026-08-28" gibi formatları DateOnly'e çevirir
    private static DateOnly ParseDate(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return DateOnly.FromDateTime(DateTime.Today);

        string[] formats =
        {
            "dd/MM/yyyy", "d/M/yyyy",
            "dd.MM.yyyy", "d.M.yyyy",
            "yyyy-MM-dd",
            "dd/MM/yy",   "d/M/yy"
        };

        foreach (var fmt in formats)
        {
            if (DateOnly.TryParseExact(raw.Trim(), fmt,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var d))
                return d;
        }

        return DateOnly.FromDateTime(DateTime.Today);
    }
}