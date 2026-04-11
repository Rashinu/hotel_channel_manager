namespace HotelChannelManager.Services;

// PMS'ten gelen cevabı temsil eder
public class PmsResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? PmsReservationId { get; set; } // PMS kendi ID'sini verir
    public string? ErrorCode { get; set; }
}