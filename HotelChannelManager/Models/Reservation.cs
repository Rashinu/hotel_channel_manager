namespace HotelChannelManager.Models;
public class Reservation
{
    public int Id { get; set;}
    public string GuestName {get; set;} = string.Empty;
    public string RoomType {get; set;} = string.Empty;
    public DateOnly CheckIn {get; set;}
    public DateOnly CheckOut {get; set;}
    public string Source {get; set;} =  "DIRECT";
    public string Status {get; set;} = "PENDING";
    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;
    public string? ErrorMessage {get; set;}
}

public class CreateReservationRequest
{
    public string GuestName {get; set;} = string.Empty;
    public string RoomType {get; set;} = string.Empty;
    public DateOnly CheckIn {get; set;}
    public DateOnly CheckOut {get; set;}
    public string Source {get; set;} =  "DIRECT";
}