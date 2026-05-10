namespace HotelChannelManager.Pratik.Services;

using HotelChannelManager.Pratik.Services.Parsers;
using UglyToad.PdfPig;

public class PdfParserService
{
    private readonly SuenoTurParser _suenoTurParser;

    public PdfParserService(SuenoTurParser suenoTurParser)
    {
        _suenoTurParser = suenoTurParser;
    }

    // PDF eki olan mailler için — şu an SUENO HTML tabanlı geldiği için bu path nadiren kullanılır
    public ParsedReservation? ParsePdf(string providerName, byte[] pdfBytes)
    {
        if (pdfBytes == null || pdfBytes.Length == 0) return null;
        try
        {
            var text = ExtractTextFromPdf(pdfBytes);
            // CORNER CASE: PDF boş veya bozuksa ExtractTextFromPdf boş string döner → parse null döner → mail FAILED olur
            return null; // TODO: OTS/MTS gibi PDF-bazlı providerlar eklenirse buraya parser gelir
        }
        catch { return null; }
    }

    // HTML body olan mailler için (SUENO TUR gibi)
    public ParsedReservation? ParseHtml(string providerName, string htmlBody)
    {
        try
        {
            return providerName switch
            {
                "SUENO" => _suenoTurParser.Parse(htmlBody),
                _       => null
            };
        }
        catch { return null; }
    }

    private static string ExtractTextFromPdf(byte[] pdfBytes)
    {
        var text = new System.Text.StringBuilder();
        using var document = PdfDocument.Open(pdfBytes);
        foreach (var page in document.GetPages())
            text.AppendLine(page.Text);
        return text.ToString();
    }
}
