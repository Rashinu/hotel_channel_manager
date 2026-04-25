namespace HotelChannelManager.Controllers;

using HotelChannelManager.Data;
using HotelChannelManager.Models;
using HotelChannelManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class FetchImapRequest
{
    public string ImapHost { get; set; } = "imap-mail.outlook.com";
    public int ImapPort { get; set; } = 993;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class SendTestMailRequest
{
    public string SmtpHost { get; set; } = "smtp.office365.com";
    public int SmtpPort { get; set; } = 587;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderPassword { get; set; } = string.Empty;
    public string ToEmail { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Provider { get; set; } = "SUENO";
    public string? Voucher { get; set; }
    public string? GuestName { get; set; }
    public string? CheckIn { get; set; }
    public string? CheckOut { get; set; }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MailController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly FakeMailboxService _fakeMailbox;
    private readonly MailIntegrationService _mailIntegration;
    private readonly SmtpSenderService _smtpSender;
    private readonly ImapMailFetchService _imapFetch;

    public MailController(
        AppDbContext context,
        FakeMailboxService fakeMailbox,
        MailIntegrationService mailIntegration,
        SmtpSenderService smtpSender,
        ImapMailFetchService imapFetch)
    {
        _context = context;
        _fakeMailbox = fakeMailbox;
        _mailIntegration = mailIntegration;
        _smtpSender = smtpSender;
        _imapFetch = imapFetch;
    }

    // GET /api/mail — Filtreleme destekli mail listesi
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? subject,
        [FromQuery] string? fromAddress,
        [FromQuery] string? toAddress,
        [FromQuery] int? mailId,
        [FromQuery] int? customerId,
        [FromQuery] int? providerId,
        [FromQuery] bool? isRead,
        [FromQuery] bool? isConverted,
        [FromQuery] bool? fromAddressRequired,
        [FromQuery] bool? toAddressRequired,
        [FromQuery] DateTime? beginReceiveDate,
        [FromQuery] DateTime? endReceiveDate,
        [FromQuery] DateTime? beginMailDate,
        [FromQuery] DateTime? endMailDate)
    {
        var query = _context.IncomingMails.AsQueryable();

        if (!string.IsNullOrWhiteSpace(subject))
            query = query.Where(m => m.Subject.Contains(subject));

        if (!string.IsNullOrWhiteSpace(fromAddress))
            query = query.Where(m => m.FromAddress.Contains(fromAddress));

        if (!string.IsNullOrWhiteSpace(toAddress))
            query = query.Where(m => m.ToAddress.Contains(toAddress));

        if (mailId.HasValue)
            query = query.Where(m => m.Id == mailId.Value);

        if (customerId.HasValue)
            query = query.Where(m => m.CustomerId == customerId.Value);

        if (providerId.HasValue)
            query = query.Where(m => m.ProviderId == providerId.Value);

        if (isRead.HasValue)
            query = query.Where(m => m.IsRead == isRead.Value);

        if (isConverted.HasValue)
        {
            var status = isConverted.Value ? "CONVERTED" : "PENDING";
            query = query.Where(m => m.Status == status);
        }

        if (fromAddressRequired == true)
            query = query.Where(m => m.FromAddress != string.Empty);

        if (toAddressRequired == true)
            query = query.Where(m => m.ToAddress != string.Empty);

        if (beginReceiveDate.HasValue)
            query = query.Where(m => m.ReceiveDate >= beginReceiveDate.Value);

        if (endReceiveDate.HasValue)
            query = query.Where(m => m.ReceiveDate <= endReceiveDate.Value);

        if (beginMailDate.HasValue)
            query = query.Where(m => m.MailDate >= beginMailDate.Value);

        if (endMailDate.HasValue)
            query = query.Where(m => m.MailDate <= endMailDate.Value);

        var mails = await query
            .OrderByDescending(m => m.ReceiveDate)
            .ToListAsync();

        return Ok(new { success = true, count = mails.Count, data = mails });
    }

    // POST /api/mail/fetch — Fake mailbox'tan mailleri çek
    [HttpPost("fetch")]
    public async Task<IActionResult> FetchMails()
    {
        var fakeMails = _fakeMailbox.GetFakeMails();
        var added = 0;

        foreach (var mail in fakeMails)
        {
            var exists = await _context.IncomingMails
                .AnyAsync(m => m.UniqueId == mail.UniqueId);

            if (!exists)
            {
                _context.IncomingMails.Add(mail);
                added++;
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = $"{added} yeni mail eklendi", total = added });
    }

    // POST /api/mail/inject-test — IMAP olmadan doğrudan DB'ye test maili yaz
    [HttpPost("inject-test")]
    public async Task<IActionResult> InjectTest()
    {
        var html = TestMailTemplates.SuenoTur(
            voucher: "600701", guestName: "ERTUGRUL SIPAHI",
            checkIn: "28/08/2026", checkOut: "03/09/2026");

        var mail = new IncomingMail
        {
            UniqueId    = $"TEST-{DateTime.UtcNow.Ticks}",
            UidKey      = "test@suenotur.com",
            MailDate    = DateTime.UtcNow,
            ReceiveDate = DateTime.UtcNow,
            FromAddress = "zbalbay@suenotur.com",
            ToAddress   = "murat_keskin_2014@hotmail.com",
            Subject     = "Rezervasyon Konfirmesi - 600701",
            Body        = html,
            HasAttachments = false,
            IsRead      = false,
            ProviderName = "SUENO",
            Status      = "PENDING"
        };

        _context.IncomingMails.Add(mail);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, mailId = mail.Id, message = "Test maili DB'ye eklendi" });
    }

    // POST /api/mail/fetch-imap — Hotmail IMAP'ten gerçek mailleri çek
    [HttpPost("fetch-imap")]
    public async Task<IActionResult> FetchImap([FromBody] FetchImapRequest req)
    {
        try
        {
            var mails = await _imapFetch.FetchUnreadAsync(
                req.ImapHost, req.ImapPort, req.Email, req.Password);

            var added = 0;
            foreach (var mail in mails)
            {
                // Aynı UID ile zaten kayıtlı mı?
                var exists = await _context.IncomingMails
                    .AnyAsync(m => m.UniqueId == mail.UniqueId);

                if (!exists)
                {
                    _context.IncomingMails.Add(mail);
                    added++;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                fetched = mails.Count,
                added,
                message = $"{mails.Count} mail çekildi, {added} yeni eklendi"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    // POST /api/mail/send-test — SMTP ile test maili gönder (simülasyon)
    [HttpPost("send-test")]
    public async Task<IActionResult> SendTest([FromBody] SendTestMailRequest req)
    {
        try
        {
            var htmlBody = req.Provider switch
            {
                "SUENO" => TestMailTemplates.SuenoTur(
                    voucher:   req.Voucher   ?? "600701",
                    guestName: req.GuestName ?? "ERTUGRUL SIPAHI",
                    checkIn:   req.CheckIn   ?? "28/08/2026",
                    checkOut:  req.CheckOut  ?? "03/09/2026"),
                "OTS" => TestMailTemplates.OtsMts(
                    voucher:   req.Voucher   ?? "D8W9YH",
                    guestName: req.GuestName ?? "Peter Smedley Karl"),
                _ => TestMailTemplates.SuenoTur()
            };

            await _smtpSender.SendAsync(
                smtpHost:       req.SmtpHost,
                smtpPort:       req.SmtpPort,
                senderEmail:    req.SenderEmail,
                senderPassword: req.SenderPassword,
                toEmail:        req.ToEmail,
                subject:        req.Subject ?? $"Rezervasyon Konfirmesi - {req.Voucher ?? "600701"}",
                htmlBody:       htmlBody);

            return Ok(new { success = true, message = $"Test maili gönderildi → {req.ToEmail}" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    // POST /api/mail/{id}/convert — Maili rezervasyona dönüştür
    [HttpPost("{id}/convert")]
    public async Task<IActionResult> Convert(int id)
    {
        var mail = await _context.IncomingMails.FindAsync(id);

        if (mail is null)
            return NotFound(new { success = false, error = "Mail bulunamadı" });

        if (mail.Status == "CONVERTED")
            return BadRequest(new { success = false, error = "Mail zaten convert edildi" });

        await _mailIntegration.ProcessMailAsync(mail, _context);

        return Ok(new { success = true, data = mail });
    }

    // PATCH /api/mail/{id}/read — Okundu olarak işaretle
    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var mail = await _context.IncomingMails.FindAsync(id);
        if (mail is null)
            return NotFound(new { success = false, error = "Mail bulunamadı" });

        mail.IsRead = true;
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }
}
