namespace HotelChannelManager.Services.Parsers;

using HotelChannelManager.Services;

public class OtsMtsParser
{
    // OTS/MTS PDF formatı:
    // "Voucher: D8W9YH"
    // "Dates: 07.Jun.26 - 17.Jun.26"
    // "Guest: Peter Smedley Karl"
    // "Room: Comfort Room"
    // "Pension: All Inclusive"

    public ParsedReservation? Parse(string text)
    {
        try
        {
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var reservation = new ParsedReservation
            {
                ProviderName = "OTS/MTS"
            };

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Voucher
                if (trimmed.StartsWith("Voucher"))
                {
                    reservation.Voucher = ExtractValue(trimmed);
                }

                // Misafir adı
                if (trimmed.StartsWith("Guests:") || trimmed.StartsWith("Guest:"))
                {
                    reservation.GuestName = ExtractValue(trimmed);
                }

                // Tarihler: "07.Jun.26 - 17.Jun.26"
                if (trimmed.StartsWith("Dates:"))
                {
                    var dates = ExtractValue(trimmed).Split('-');
                    if (dates.Length == 2)
                    {
                        reservation.CheckIn = ParseDate(dates[0].Trim());
                        reservation.CheckOut = ParseDate(dates[1].Trim());
                    }
                }

                // Oda tipi
                if (trimmed.StartsWith("Service:") || trimmed.StartsWith("Room:"))
                {
                    reservation.RoomType = ExtractValue(trimmed);
                }

                // Pansiyon
                if (trimmed.Contains("All Inclusive"))
                {
                    reservation.Pension = "All Inclusive";
                }

                // Rezervasyon tipi
                if (trimmed.Contains("New Bookings"))
                    reservation.ReservationType = "NEW";
                else if (trimmed.Contains("Cancelled"))
                    reservation.ReservationType = "CANCELLED";
                else if (trimmed.Contains("Changed"))
                    reservation.ReservationType = "CHANGED";
            }

            return reservation;
        }
        catch
        {
            return null;
        }
    }

    // "Voucher: D8W9YH" → "D8W9YH"
    private string ExtractValue(string line)
    {
        var parts = line.Split(':', 2);
        return parts.Length > 1 ? parts[1].Trim() : string.Empty;
    }

    private DateOnly ParseDate(string dateStr)
    {
        // "07.Jun.26" formatını parse et
        if (DateTime.TryParse(dateStr, out var date))
            return DateOnly.FromDateTime(date);

        return DateOnly.FromDateTime(DateTime.Today);
    }
}