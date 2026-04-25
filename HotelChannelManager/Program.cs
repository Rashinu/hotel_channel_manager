using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using HotelChannelManager.Services;
using HotelChannelManager.Data;
using Microsoft.EntityFrameworkCore;
using HotelChannelManager.Models;
using Serilog;
using HotelChannelManager.Services.Parsers;
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/hotel-channel-manager-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();


var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

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
            Array.Empty<string>()
        }
    });
});



// TokenService'i kaydet
builder.Services.AddScoped<TokenService>();
builder.Services.AddSingleton<InMemoryStore>(); 
// Queue → Singleton olmalı, her yerden aynı kuyruk erişilmeli
builder.Services.AddSingleton<ReservationQueue>();
builder.Services.AddSingleton<FakeOtaService>();
builder.Services.AddSingleton<FakePmsService>();
builder.Services.AddScoped<FakeMailboxService>();
builder.Services.AddScoped<MailIntegrationService>();
builder.Services.AddScoped<SmtpSenderService>();
builder.Services.AddScoped<ImapMailFetchService>();
builder.Services.AddScoped<SuenoTurParser>();
// Worker → Arka planda sürekli çalışacak
builder.Services.AddHostedService<ReservationWorker>();
builder.Services.AddScoped<PdfParserService>();
builder.Services.AddScoped<OtsMtsParser>();
builder.Services.AddScoped<JollyturParser>();
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

// Uygulama başlarken fake entegrasyonlar ekle
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!context.ProviderIntegrations.Any())
    {
        context.ProviderIntegrations.AddRange(
            new ProviderIntegration
            {
                ProviderName = "OTS/MTS",
                Username = "reservation@monart.com.tr",
                Password = "test123",
                VoucherType = "Full",
                Interval = 2,
                IsActive = true
            },
            new ProviderIntegration
            {
                ProviderName = "ORS",
                Username = "ors@hotel.com",
                Password = "ors123",
                VoucherType = "Operator",
                Interval = 2,
                IsActive = true
            }
        );

        await context.SaveChangesAsync();
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseDefaultFiles();   // index.html'i default yap
app.UseStaticFiles();    // wwwroot servis et

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => new { status = "OK", timestamp = DateTime.UtcNow });

app.Run();