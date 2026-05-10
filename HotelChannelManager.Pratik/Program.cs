using HotelChannelManager.Pratik.Data;
using HotelChannelManager.Pratik.Services;
using HotelChannelManager.Pratik.Services.Parsers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite("Data Source=pratik.db"));

builder.Services.AddScoped<SuenoTurParser>();
builder.Services.AddScoped<PdfParserService>();
builder.Services.AddScoped<ImapMailFetchService>();
builder.Services.AddScoped<MailIntegrationService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseHttpsRedirection();

// Okunmamış mailleri IMAP'tan çek ve DB'ye kaydet
app.MapPost("/fetch-mails", async (ImapMailFetchService fetcher, AppDbContext db, IConfiguration config) =>
{
    var host     = config["Imap:Host"]     ?? throw new InvalidOperationException("Imap:Host eksik");
    var port     = int.Parse(config["Imap:Port"] ?? "993");
    var email    = config["Imap:Email"]    ?? throw new InvalidOperationException("Imap:Email eksik");
    var password = config["Imap:Password"] ?? throw new InvalidOperationException("Imap:Password eksik");

    var mails = await fetcher.FetchUnreadAsync(host, port, email, password);

    // CORNER CASE: Aynı mail 2 kez fetch edilirse UidKey kontrolü ile duplicate önlenir
    var existingKeys = db.IncomingMails.Select(m => m.UidKey).ToHashSet();
    var newMails = mails.Where(m => !existingKeys.Contains(m.UidKey)).ToList();

    db.IncomingMails.AddRange(newMails);
    await db.SaveChangesAsync();

    return Results.Ok(new { Fetched = mails.Count, Saved = newMails.Count });
});

// DB'deki PENDING mailleri rezervasyona dönüştür
app.MapPost("/process-mails", async (MailIntegrationService integrator, AppDbContext db) =>
{
    var pending = db.IncomingMails.Where(m => m.Status == "PENDING").ToList();

    var results = new List<object>();
    foreach (var mail in pending)
    {
        var (success, reservation) = await integrator.ProcessMailAsync(mail);
        results.Add(new { mail.Id, mail.FromAddress, success, voucher = reservation?.Voucher });
    }

    return Results.Ok(results);
});

// Tüm mailleri listele
app.MapGet("/mails", (AppDbContext db) =>
    Results.Ok(db.IncomingMails.OrderByDescending(m => m.ReceiveDate).ToList()));

// Tüm rezervasyonları listele
app.MapGet("/reservations", (AppDbContext db) =>
    Results.Ok(db.Reservations.OrderByDescending(r => r.CreatedAt).ToList()));

app.Run();
