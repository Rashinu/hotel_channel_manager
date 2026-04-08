namespace HotelChannelManager.Data;
using HotelChannelManager.Models;
public class InMemoryStore
{
    private readonly List<Reservation> _reservations = new();
    private int _nextId = 1;

    public List<Reservation> GetAll() => _reservations.ToList();

    public Reservation? GetById(int id) =>
        _reservations.FirstOrDefault(r => r.Id == id);

    public Reservation Create(Reservation reservation)
    {
        reservation.Id = _nextId++;
        _reservations.Add(reservation);
        return reservation;
    }

    public Reservation? Update(int id, Action<Reservation> updateAction)
    {
        var reservation = GetById(id);
        if (reservation is null) return null;
        updateAction(reservation);
        return reservation;
    }
}

