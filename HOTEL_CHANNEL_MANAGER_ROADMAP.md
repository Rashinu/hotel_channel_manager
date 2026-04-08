# 🏨 Hotel Channel Manager — Kişisel Öğrenme Yol Haritası
**Seviye:** Orta C# | **Günlük süre:** 1.5 saat | **Hedef:** Pozisyon değişikliği (içeriden veya dışarıdan)

---

## 📌 Bu Doküman Ne İçin?

Resbox benzeri bir sistemde çalışıyorsun. Her gün API hatalarını, rezervasyon senkronizasyon problemlerini ve PMS entegrasyonlarını görüyorsun. Hedefin bu sistemleri **dışarıdan raporlamak** yerine **içeriden inşa edebilmek**.

Bu yol haritası seni oraya götürecek. Adım adım. Atlamadan.

---

## ⚠️ Kurallar

1. **Bir fazı bitirmeden diğerine geçme**
2. **Kodu yapıştırma — yaz, sonra anla**
3. **Her gün 1.5 saat — az ama düzenli**
4. **Takılırsan Claude'a sor, ama önce 15 dakika kendin dene**

---

## 🗺 Genel Harita

```
FAZ 1 → Basit API (1 hafta)
FAZ 2 → JWT Authentication (1 hafta)
FAZ 3 → Veritabanı - EF Core (1.5 hafta)
FAZ 4 → Queue & Worker Sistemi (1.5 hafta)
FAZ 5 → Dış Servis Entegrasyonu (1 hafta)
FAZ 6 → Hata Yönetimi & Logging (1 hafta)
──────────────────────────────
Toplam: ~8 hafta → CEO'ya git
```

---

## FAZ 1 — Basit REST API
**Süre:** 1 hafta (7 × 1.5 saat = ~10 saat)
**Araç:** Antigravity (VS Code tabanlı)

### Ne Öğreneceksin?
- ASP.NET Core Web API nasıl çalışır
- HTTP metodları: GET, POST, PATCH
- Controller, Route, Model nedir
- HTTP status kodları (200, 201, 400, 404)

### Resbox'taki Karşılığı
> Booking.com bir rezervasyon gönderdiğinde, bu endpoint'e POST atar.
> Sen şu an bu POST'u log'da görüyorsun. Bu fazda o endpoint'i kendin yazacaksın.

### Gün 1-2: Proje Kurulumu
Antigravity terminalinde:
```bash
dotnet new webapi -n HotelChannelManager
cd HotelChannelManager
dotnet run
```
`http://localhost:5000/swagger` açılıyorsa kurulum tamam.

### Gün 3-4: İlk Model ve Controller

**Models/Reservation.cs**
```csharp
public class Reservation
{
    public int Id { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public string Source { get; set; } = "DIRECT";
    public string Status { get; set; } = "PENDING";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }
}

public class CreateReservationRequest
{
    public string GuestName { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public string Source { get; set; } = "DIRECT";
}
```

**Data/InMemoryStore.cs**
```csharp
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
```

**Controllers/ReservationsController.cs**
```csharp
[ApiController]
[Route("api/[controller]")]
public class ReservationsController : ControllerBase
{
    private readonly InMemoryStore _store;

    public ReservationsController(InMemoryStore store)
    {
        _store = store;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var list = _store.GetAll();
        return Ok(new { success = true, count = list.Count, data = list });
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var reservation = _store.GetById(id);
        if (reservation is null)
            return NotFound(new { success = false, error = "Reservation not found", id });

        return Ok(new { success = true, data = reservation });
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateReservationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.GuestName))
            return BadRequest(new { success = false, error = "GuestName is required" });

        if (request.CheckOut <= request.CheckIn)
            return BadRequest(new { success = false, error = "CheckOut must be after CheckIn" });

        var reservation = new Reservation
        {
            GuestName = request.GuestName,
            RoomType = request.RoomType,
            CheckIn = request.CheckIn,
            CheckOut = request.CheckOut,
            Source = request.Source
        };

        var created = _store.Create(reservation);
        return CreatedAtAction(nameof(GetById), new { id = created.Id },
            new { success = true, data = created });
    }

    [HttpPatch("{id}/status")]
    public IActionResult UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        var valid = new[] { "PENDING", "CONFIRMED", "FAILED", "CANCELLED" };
        if (!valid.Contains(request.Status))
            return BadRequest(new { success = false, error = "Invalid status", valid });

        var updated = _store.Update(id, r =>
        {
            r.Status = request.Status;
            if (request.ErrorMessage is not null)
                r.ErrorMessage = request.ErrorMessage;
        });

        if (updated is null)
            return NotFound(new { success = false, error = "Reservation not found" });

        return Ok(new { success = true, data = updated });
    }
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}
```

**Program.cs**
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<InMemoryStore>(); // ← BU ÖNEMLİ

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapGet("/health", () => new { status = "OK", timestamp = DateTime.UtcNow });
app.Run();
```

### Gün 5-7: Test Et

Swagger'dan veya Postman'den test:
```
POST /api/reservations
{
  "guestName": "Ahmet Yılmaz",
  "roomType": "DOUBLE",
  "checkIn": "2026-07-10",
  "checkOut": "2026-07-15",
  "source": "BOOKING_COM"
}
```

### ✅ Faz 1 Görevleri (Geçmeden önce bunları yap)
- [ ] Geçersiz tarih gönder → 400 alıyor musun?
- [ ] `GET /api/reservations/999` → 404 alıyor musun?
- [ ] Rezervasyon oluştur → Status'u `FAILED` yap → ErrorMessage: `"PMS connection timeout"`
- [ ] `/health` endpoint'i çalışıyor mu?

---

## FAZ 2 — JWT Authentication
**Süre:** 1 hafta
**Ne öğreneceksin:** Token nedir, nasıl üretilir, nasıl doğrulanır

### Resbox'taki Karşılığı
> Şu an destek verirken "Unauthorized" hatası görüyorsun.
> Bu fazda o hatayı sen üreteceksin — ve neden olduğunu kod seviyesinde anlayacaksın.

### Eklenecekler
```
Models/
  └── User.cs
  └── LoginRequest.cs
  └── TokenResponse.cs
Controllers/
  └── AuthController.cs
Services/
  └── TokenService.cs
```

### NuGet Paketi
```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

### Temel Akış
```
POST /api/auth/login
  → Email + Password gönder
  → Token al
  → Diğer endpoint'lere Authorization: Bearer {token} ile git
  → Token yoksa → 401 Unauthorized
```

### ✅ Faz 2 Görevleri
- [ ] Token olmadan `GET /api/reservations` → 401 alıyor musun?
- [ ] Yanlış şifre → 401 alıyor musun?
- [ ] Süresi dolmuş token → 401 alıyor musun?
- [ ] Geçerli token → Rezervasyon listesi geliyor mu?

---

## FAZ 3 — Veritabanı (Entity Framework Core)
**Süre:** 1.5 hafta
**Ne öğreneceksin:** ORM nedir, migration nedir, LINQ sorguları

### Resbox'taki Karşılığı
> Şu an InMemoryStore kullanıyoruz — uygulama kapanınca her şey siliniyor.
> Bu fazda gerçek bir veritabanına geçiyoruz.
> Elektra'nın veriyi nasıl sakladığını bu fazda anlayacaksın.

### Eklenecekler
```bash
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Tools
```

### Temel Akış
```
Entity (Model) → DbContext → Migration → SQLite Dosyası
```

### ✅ Faz 3 Görevleri
- [ ] Migration oluştur ve çalıştır
- [ ] Rezervasyon oluştur → Uygulamayı kapat → Tekrar aç → Veri hâlâ duruyor mu?
- [ ] SQL ile direkt veritabanına bak → Rezervasyon orada mı?
- [ ] Status'a göre filtrele: `GET /api/reservations?status=FAILED`

---

## FAZ 4 — Queue & Worker Sistemi
**Süre:** 1.5 hafta
**Ne öğreneceksin:** Background service nedir, queue mantığı nedir

### Resbox'taki Karşılığı
> OTA'dan rezervasyon geldiğinde sistem hemen PMS'e iletmez.
> Önce kuyruğa alır, worker arka planda işler.
> "Rezervasyon neden düşmedi?" sorusunun cevabı çoğunlukla burada.

### Temel Akış
```
POST /api/reservations
  → Rezervasyon PENDING olarak kaydedilir
  → Kuyruğa eklenir
  → Worker arka planda çalışır
  → PMS'e iletmeye çalışır
  → Başarılı → CONFIRMED
  → Başarısız → FAILED + ErrorMessage
```

### ✅ Faz 4 Görevleri
- [ ] Rezervasyon oluştur → 3 saniye bekle → Status CONFIRMED oldu mu?
- [ ] Worker'ı kasıtlı hata ver → Status FAILED oldu mu?
- [ ] 5 rezervasyon aynı anda gönder → Hepsi işlendi mi?
- [ ] Log'da worker çalışmasını görebiliyor musun?

---

## FAZ 5 — Dış Servis Entegrasyonu (Fake OTA & PMS)
**Süre:** 1 hafta
**Ne öğreneceksin:** HttpClient, retry mekanizması, timeout

### Resbox'taki Karşılığı
> Booking.com → Resbox → Elektra zincirinin tamamı bu fazda.
> İki fake servis yazacaksın: biri OTA simüle eder, biri PMS.
> 503 hatasını, timeout'u, mapping hatasını bizzat üreteceksin.

### Fake Servisler
```
FakeOTA → Booking.com simülasyonu
  → Bazen 200 döner
  → Bazen 503 döner
  → Bazen yanlış format gönderir (mapping hatası)

FakePMS → Elektra simülasyonu
  → Rezervasyonu kabul eder
  → Bazen "room not found" hatası verir
```

### ✅ Faz 5 Görevleri
- [ ] FakeOTA'dan 503 al → Sistem nasıl davranıyor?
- [ ] Mapping hatası üret → ErrorMessage doğru kaydedildi mi?
- [ ] FakePMS'e bağlantı kopar → Retry çalışıyor mu?
- [ ] Başarılı end-to-end: OTA → Queue → Worker → PMS → CONFIRMED

---

## FAZ 6 — Hata Yönetimi & Logging
**Süre:** 1 hafta
**Ne öğreneceksin:** Serilog, global exception handler, structured logging

### Resbox'taki Karşılığı
> Şu an destek verirken log'lara bakıyorsun.
> Bu fazda o log'ları sen yazacaksın.
> "Rezervasyon neden düşmedi?" sorusunun cevabı artık log'unda olacak.

### Eklenecekler
```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.File
```

### Log Örnekleri
```
[2026-07-10 14:23:01] INFO  Reservation received | Source: BOOKING_COM | Guest: Ahmet Yılmaz
[2026-07-10 14:23:01] INFO  Added to queue | ReservationId: 42
[2026-07-10 14:23:04] INFO  Worker processing | ReservationId: 42
[2026-07-10 14:23:05] ERROR PMS connection failed | ReservationId: 42 | Error: timeout
[2026-07-10 14:23:05] INFO  Retry scheduled | ReservationId: 42 | Attempt: 1/3
```

### ✅ Faz 6 Görevleri
- [ ] Log dosyasını aç → Rezervasyon akışını takip edebiliyor musun?
- [ ] Kasıtlı hata üret → Hata log'da görünüyor mu?
- [ ] 401 hatası → Log'da görünüyor mu?
- [ ] Bir rezervasyonun tüm akışını log'dan okuyabildin mi?

---

## 🎯 Faz 6 Bittikten Sonra

Elinde şunlar olacak:

```
✅ Çalışan bir ASP.NET Core Web API
✅ JWT Authentication
✅ Gerçek veritabanı (SQLite → sonra SQL Server)
✅ Queue & Worker sistemi
✅ Fake OTA + PMS entegrasyonu
✅ Hata simülasyonu (401, 503, mapping)
✅ Structured logging
```

### CEO'ya Gidince Şunu De:
> "Boş zamanımda Resbox'a benzer bir sistem yazdım.
> OTA'dan rezervasyon alıyor, kuyruğa atıyor, worker PMS'e iletiyor.
> 503 hatasını, mapping hatasını, token hatasını simüle ettim.
> Bakabilir misin?"

Bu cümlenin arkası dolu.

---

## 📅 Haftalık Hedefler

| Hafta | Hedef | Kontrol |
|-------|-------|---------|
| 1 | Faz 1 tamamla | Swagger'dan tüm endpoint'ler çalışıyor |
| 2 | Faz 2 tamamla | 401 hatası üretebiliyorsun |
| 3-4 | Faz 3 tamamla | Veri veritabanında kalıcı |
| 5-6 | Faz 4 tamamla | Worker arka planda çalışıyor |
| 7 | Faz 5 tamamla | End-to-end akış çalışıyor |
| 8 | Faz 6 tamamla | Log'dan rezervasyon takip edebiliyorsun |

---

## 🆘 Takılınca Ne Yaparsın?

1. **15 dakika kendin dene**
2. **Hata mesajını Google'la** → Stack Overflow'da çözüm ara
3. **Claude'a sor** → Hata mesajını + ne yapmaya çalıştığını söyle
4. **Bir önceki çalışan haline dön** → Üzerine tekrar yaz

---

## 💡 Önemli Not

Bu projeyi mükemmel yapmak zorunda değilsin.

**Çalışması yeterli.**

Senior developer da ilk yazdığında böyle bir proje yazar. Fark: senior developer vazgeçmez.

---

*Yol haritası sürüm: 1.0 | Hazırlandığı tarih: Mart 2026*
*Hedef: Hotel Channel Manager Backend Developer*
