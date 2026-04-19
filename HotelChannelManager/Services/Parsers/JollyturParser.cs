namespace HotelChannelManager.Services.Parsers;

using HotelChannelManager.Services;
using HtmlAgilityPack;

public class JollyturParser
{
    // Jollytur HTML formatı:
    // ContentFileUrl → HTML sayfası
    // İçinde "tıklayınız" linki var
    // O linke git → Rezervasyon fişi HTML'i gelir

    public ParsedReservation? Parse(string html)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var reservation = new ParsedReservation
            {
                ProviderName = "JOLLYTUR"
            };

            // Tüm tablo satırlarını gez
            var rows = doc.DocumentNode.SelectNodes("//tr");
            if (rows == null) return null;

            foreach (var row in rows)
            {
                var cells = row.SelectNodes("td");
                if (cells == null || cells.Count < 2) continue;

                var label = cells[0].InnerText.Trim();
                var value = cells[1].InnerText.Trim();

                // Rezervasyon Kodu
                if (label.Contains("Rezervasyon") && label.Contains("Kodu"))
                    reservation.Voucher = value;

                // Giriş Tarihi
                if (label.Contains("Giriş Tarihi"))
                {
                    if (DateOnly.TryParse(value, out var checkIn))
                        reservation.CheckIn = checkIn;
                }

                // Çıkış Tarihi
                if (label.Contains("Çıkış Tarihi"))
                {
                    if (DateOnly.TryParse(value, out var checkOut))
                        reservation.CheckOut = checkOut;
                }

                // Oda tipi
                if (label.Contains("Konaklama") || label.Contains("Oda"))
                    reservation.RoomType = value;

                // Pansiyon
                if (label.Contains("Pansiyon"))
                    reservation.Pension = value;
            }

            // Misafir isimlerini çek
            var guestRows = doc.DocumentNode
                .SelectNodes("//table//tr[td[contains(@class,'guest')]]");

            if (guestRows != null && guestRows.Count > 0)
            {
                var firstGuest = guestRows[0].SelectNodes("td");
                if (firstGuest != null && firstGuest.Count > 1)
                    reservation.GuestName = firstGuest[1].InnerText.Trim();
            }

            // Rezervasyon tipi
            var pageText = doc.DocumentNode.InnerText;
            if (pageText.Contains("İptal") || pageText.Contains("Cancelled"))
                reservation.ReservationType = "CANCELLED";
            else if (pageText.Contains("Değişiklik") || pageText.Contains("Changed"))
                reservation.ReservationType = "CHANGED";
            else
                reservation.ReservationType = "NEW";

            return reservation;
        }
        catch
        {
            return null;
        }
    }
}