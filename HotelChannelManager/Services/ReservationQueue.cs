namespace HotelChannelManager.Services;

using HotelChannelManager.Models;
using System.Collections.Concurrent;

// ConcurrentQueue → Aynı anda birden fazla istek gelse bile
// sıra bozulmaz, veri kaybolmaz
// Gerçek sistemde: Redis, RabbitMQ gibi araçlar bu işi yapar
// Biz şimdilik bellekte tutuyoruz
public class ReservationQueue
{
    private readonly ConcurrentQueue<Reservation> _queue = new();

    // Kuyruğa ekle
    // Booking.com rezervasyon gönderdiğinde buraya düşer
    public void Enqueue(Reservation reservation)
    {
        _queue.Enqueue(reservation);
    }

    // Kuyruktan al
    // Worker buradan alır ve işler
    public bool TryDequeue(out Reservation? reservation)
    {
        return _queue.TryDequeue(out reservation);
    }

    // Kuyrukta kaç rezervasyon var?
    public int Count => _queue.Count;
}