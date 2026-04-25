namespace HotelChannelManager.Services;

public static class TestMailTemplates
{
    // SUENO TUR formatında gerçek bir rezervasyon maili
    public static string SuenoTur(
        string voucher     = "600701",
        string guestName   = "ERTUGRUL SIPAHI",
        string roomType    = "DOUBLE +1 CHD",
        string category    = "DELUXE DENIZ STD",
        string checkIn     = "28/08/2026",
        string checkOut    = "03/09/2026",
        string pension     = "AI",
        string agency      = "WEBACENTAM ACENTALAR",
        string seller      = "TATILBILIR TURIZ",
        string total       = "105.800,00",
        string transport   = "KENDI",
        string date        = "18/04/2026")
    {
        return $@"<!DOCTYPE html>
<html>
<head>
<meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"">
<style>
body {{ font-family: Calibri, Arial, sans-serif; font-size: 11pt; }}
table {{ border-collapse: collapse; width: 100%; }}
th {{ background-color: #4472C4; color: white; border: 1px solid #333;
      padding: 4px 6px; font-size: 9pt; text-align: center; }}
td {{ border: 1px solid #999; padding: 4px 6px; font-size: 9pt; }}
.header {{ font-weight: bold; font-size: 12pt; text-align: center; margin-bottom: 10px; }}
.greeting {{ margin-bottom: 12px; }}
.total-row {{ font-weight: bold; }}
</style>
</head>
<body>
<div class=""header"">SUENO TUR 2026 &nbsp;&nbsp; {date} 15:32</div>
<div class=""header"">SUENO DELUXE BELEK /ANTALYA</div>

<p class=""greeting"">
SAYIIN REZERVASYON YETKİLİSİ,<br/>
Asagidaki Rezervasyonun Konfirmesini rica eder, iyi calismalar dilerim..
</p>

<table>
  <thead>
    <tr>
      <th>VOUCHER KODU</th>
      <th>YOLCU ADI SOYADI</th>
      <th>P.D.</th>
      <th>ODA</th>
      <th>ACIKLAMA</th>
      <th>KATEGORİ</th>
      <th>DATEİN</th>
      <th>DATEOUT</th>
      <th>ULASIM</th>
      <th>BURO</th>
      <th>SATICI</th>
      <th>İNDİRİM</th>
      <th>TUTAR</th>
      <th>KAYIT GUNU</th>
    </tr>
  </thead>
  <tbody>
    <tr class=""total-row"">
      <td>{voucher}</td>
      <td>{guestName}</td>
      <td>{pension}</td>
      <td>{roomType}</td>
      <td></td>
      <td>{category}</td>
      <td>{checkIn}</td>
      <td>{checkOut}</td>
      <td>{transport}</td>
      <td>{agency}</td>
      <td>{seller}</td>
      <td></td>
      <td>{total}</td>
      <td>{date}</td>
    </tr>
  </tbody>
</table>

<br/>
<p style=""font-size:10pt; color:#333;"">
<strong>Zeynep BALBAY</strong> | Satis Temsilcisi | SuenoTur<br/>
T +90 4446677 | F +90 2122693169<br/>
<a href=""http://www.sueno.com.tr"">www.sueno.com.tr</a>
</p>

<hr/>
<p style=""font-size:8pt; color:#777;"">
Bu elektronik posta mesajı kisisel ve gizlidir.
</p>
</body>
</html>";
    }

    // OTS/MTS formatı (ileride eklenecek)
    public static string OtsMts(
        string voucher   = "D8W9YH",
        string guestName = "Peter Smedley Karl",
        string roomType  = "Comfort Room",
        string checkIn   = "07.Jun.26",
        string checkOut  = "17.Jun.26",
        string pension   = "All Inclusive")
    {
        return $@"Voucher: {voucher}
Guest: {guestName}
Dates: {checkIn} - {checkOut}
Room: {roomType}
Pension: {pension}
New Bookings";
    }
}
