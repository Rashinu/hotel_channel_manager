namespace HotelChannelManager.Pratik.Services.Parsers;

using HtmlAgilityPack;
using System.Globalization;

// Giriş : HTML string
// Çıkış : ParsedReservation (tüm alanlar dolu) ya da null

public class ParsedReservation
{
    public string Voucher { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public string Pension { get; set; } = string.Empty;
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public string ReservationType { get; set; } = "NEW";
    public int AdultCount { get; set; } = 2;
    public int ChildCount { get; set; }
    public string ProviderName { get; set; } = string.Empty;
}

public class SuenoTurParser
{
    public ParsedReservation? Parse(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var rows = doc.DocumentNode.SelectNodes("//table//tr");
        if (rows == null || rows.Count < 2) return null;

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

        if (headerIndex < 0 || headerIndex + 1 >= rows.Count) return null;

        int idxVoucher  = IndexOf(headers, "VOUCHER");
        int idxGuest    = IndexOf(headers, "YOLCU");
        int idxPension  = IndexOf(headers, "P.D.");
        int idxRoom     = IndexOf(headers, "ODA");
        int idxCategory = IndexOf(headers, "KATEG");
        int idxDateIn   = IndexOf(headers, "DATEIN", "DATEİN");
        int idxDateOut  = IndexOf(headers, "DATEOUT");

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
            RoomType        = GetCell(data, idxRoom),
            CheckIn         = ParseDate(GetCell(data, idxDateIn)),
            CheckOut        = ParseDate(GetCell(data, idxDateOut))
        };
    }

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

    private static string GetCell(List<string> data, int idx)
    {
        if (idx < 0 || idx >= data.Count) return string.Empty;
        return data[idx];
    }

    private static string CleanText(string raw)
    {
        var decoded = System.Net.WebUtility.HtmlDecode(raw ?? string.Empty);
        return System.Text.RegularExpressions.Regex.Replace(decoded, @"\s+", " ").Trim();
    }

    private static DateOnly ParseDate(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return DateOnly.FromDateTime(DateTime.Today);

        string[] formats = { "dd/MM/yyyy", "dd.MM.yyyy", "yyyy-MM-dd" };

        foreach (var fmt in formats)
        {
            if (DateOnly.TryParseExact(raw, fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return date;
        }

        return DateOnly.FromDateTime(DateTime.Today);
    }
}
