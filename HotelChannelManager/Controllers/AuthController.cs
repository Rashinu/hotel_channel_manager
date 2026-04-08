namespace HotelChannelManager.Controllers;

using HotelChannelManager.Models;
using HotelChannelManager.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly TokenService _tokenService;

    public AuthController(TokenService tokenService)
    {
        _tokenService = tokenService;
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var result = _tokenService.Login(request);

        // Token null geldiyse kullanıcı bulunamadı
        if (result is null)
            return Unauthorized(new
            {
                success = false,
                error = "Email veya şifre hatalı"
            });

        return Ok(new
        {
            success = true,
            data = result
        });
    }
}