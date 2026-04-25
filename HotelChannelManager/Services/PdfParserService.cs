namespace HotelChannelManager.Services;

using HotelChannelManager.Models;
using HotelChannelManager.Services.Parsers;
using UglyToad.PdfPig;

public class PdfParserService
{
    private readonly ILogger<PdfParserService> _logger;
    private readonly OtsMtsParser _otsMtsParser;
    private readonly JollyturParser _jollyturParser;
    private readonly SuenoTurParser _suenoTurParser;

    public PdfParserService(
        ILogger<PdfParserService> logger,
        OtsMtsParser otsMtsParser,
        JollyturParser jollyturParser,
        SuenoTurParser suenoTurParser)
    {
        _logger = logger;
        _otsMtsParser = otsMtsParser;
        _jollyturParser = jollyturParser;
        _suenoTurParser = suenoTurParser;
    }

    // PDF eki olan mailler için (OTS/MTS gibi)
    public ParsedReservation? ParsePdf(string providerName, byte[] pdfBytes)
    {
        _logger.LogInformation("PDF parse ediliyor: Provider={Provider}", providerName);
        try
        {
            var text = ExtractTextFromPdf(pdfBytes);
            return providerName switch
            {
                "OTS/MTS" => _otsMtsParser.Parse(text),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PDF parse hatası: Provider={Provider}", providerName);
            return null;
        }
    }

    // HTML body olan mailler için (SUENO TUR, JOLLYTUR gibi)
    public ParsedReservation? ParseHtml(string providerName, string htmlBody)
    {
        _logger.LogInformation("HTML parse ediliyor: Provider={Provider}", providerName);
        try
        {
            return providerName switch
            {
                "SUENO"    => _suenoTurParser.Parse(htmlBody),
                "JOLLYTUR" => _jollyturParser.Parse(htmlBody),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTML parse hatası: Provider={Provider}", providerName);
            return null;
        }
    }

    // Geriye uyum için eski imza korunuyor
    public ParsedReservation? Parse(string providerName, byte[] pdfBytes)
        => ParsePdf(providerName, pdfBytes);

    private static string ExtractTextFromPdf(byte[] pdfBytes)
    {
        var text = new System.Text.StringBuilder();
        using var document = PdfDocument.Open(pdfBytes);
        foreach (var page in document.GetPages())
            text.AppendLine(page.Text);
        return text.ToString();
    }
}

// Parse edilen rezervasyon verisi
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
