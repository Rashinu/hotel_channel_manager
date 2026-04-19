namespace HotelChannelManager.Services;

using HotelChannelManager.Models;
using HotelChannelManager.Services.Parsers;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

public class PdfParserService
{
    private readonly ILogger<PdfParserService> _logger;
    private readonly OtsMtsParser _otsMtsParser;
    private readonly JollyturParser _jollyturParser;

    public PdfParserService(
        ILogger<PdfParserService> logger,
        OtsMtsParser otsMtsParser,
        JollyturParser jollyturParser)
    {
        _logger = logger;
        _otsMtsParser = otsMtsParser;
        _jollyturParser = jollyturParser;
    }

    public ParsedReservation? Parse(string providerName, byte[] pdfBytes)
    {
        _logger.LogInformation(
            "PDF parse ediliyor: Provider={Provider}", providerName);

        try
        {
            // PDF'ten tüm metni çek
            var text = ExtractTextFromPdf(pdfBytes);

            _logger.LogInformation(
                "PDF metni çekildi: {Length} karakter", text.Length);

            // Provider'a göre doğru parser'ı çalıştır
            return providerName switch
            {
                "OTS/MTS" => _otsMtsParser.Parse(text),
                "JOLLYTUR" => _jollyturParser.Parse(text),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "PDF parse hatası: Provider={Provider}", providerName);
            return null;
        }
    }

    private string ExtractTextFromPdf(byte[] pdfBytes)
    {
        var text = new System.Text.StringBuilder();

        using var document = PdfDocument.Open(pdfBytes);
        foreach (var page in document.GetPages())
        {
            text.AppendLine(page.Text);
        }

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
    public int AdultCount { get; set; }
    public int ChildCount { get; set; }
    public string ProviderName { get; set; } = string.Empty;
}