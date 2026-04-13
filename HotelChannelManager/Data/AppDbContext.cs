namespace HotelChannelManager.Data;

using HotelChannelManager.Models;
using Microsoft.EntityFrameworkCore;

// DbContext → Veritabanı ile konuşmamızı sağlayan köprü
// Gerçek sistemde: Elektra, Fidelio gibi PMS'lerin arkasında böyle bir yapı var
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSet → Veritabanındaki tablo
    // Reservations → "reservations" tablosu olacak
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<ProviderIntegration> ProviderIntegrations { get; set; }
    public DbSet<IncomingMail> IncomingMails { get; set; }
}