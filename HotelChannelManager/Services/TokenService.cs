namespace HotelChannelManager.Services;

using HotelChannelManager.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class TokenService
{
    // Fake kullanıcılar — Faz 3'te veritabanına taşıyacağız
    private readonly List<User> _users = new()
    {
        new User { Id = 1, Email = "admin@hotel.com", Password = "123456", Role = "Admin" },
        new User { Id = 2, Email = "ota@booking.com", Password = "booking123", Role = "OTA" }
    };

    private readonly string _secretKey = "bu-cok-gizli-bir-anahtar-32-karakter!!";

    public TokenResponse? Login(LoginRequest request)
    {
        // Kullanıcıyı bul
        var user = _users.FirstOrDefault(u =>
            u.Email == request.Email &&
            u.Password == request.Password);

        // Bulunamazsa null döndür → 401 olacak
        if (user is null) return null;

        // Token üret
        var token = GenerateToken(user);

        return new TokenResponse
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            Role = user.Role
        };
    }

    private string GenerateToken(User user)
    {
        // Token içine koyacağımız bilgiler
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        // İmzalama anahtarı
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Token oluştur
        var token = new JwtSecurityToken(
            issuer: "HotelChannelManager",
            audience: "HotelChannelManagerClient",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}