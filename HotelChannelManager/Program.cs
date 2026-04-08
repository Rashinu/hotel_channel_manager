using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using HotelChannelManager.Services;
using HotelChannelManager.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Bearer {token}"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// TokenService'i kaydet
builder.Services.AddScoped<TokenService>();
builder.Services.AddSingleton<InMemoryStore>(); 
// Queue → Singleton olmalı, her yerden aynı kuyruk erişilmeli
builder.Services.AddSingleton<ReservationQueue>();

// Worker → Arka planda sürekli çalışacak
builder.Services.AddHostedService<ReservationWorker>();
// SQLite veritabanı — dosya olarak kaydedilir
// Faz 4'te SQL Server'a geçebiliriz
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=hotelchannelmanager.db"));

// JWT ayarları
var secretKey = "bu-cok-gizli-bir-anahtar-32-karakter!!";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "HotelChannelManager",
            ValidAudience = "HotelChannelManagerClient",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secretKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication(); // ← Önce Authentication
app.UseAuthorization();  // ← Sonra Authorization

app.MapControllers();
app.MapGet("/health", () => new { status = "OK", timestamp = DateTime.UtcNow });

app.Run();