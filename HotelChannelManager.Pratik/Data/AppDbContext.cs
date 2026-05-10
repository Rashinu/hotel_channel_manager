namespace HotelChannelManager.Pratik.Data;

using HotelChannelManager.Pratik.Models;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<IncomingMail> IncomingMails => Set<IncomingMail>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
}
