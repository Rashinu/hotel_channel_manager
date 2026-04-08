namespace HotelChannelManager.Models;

public class TokenResponse
{
    public string Token {get;set;} = string.Empty;
    public DateTime ExpiresAt {get;set;}
    public string Role {get;set;} = string.Empty;
}