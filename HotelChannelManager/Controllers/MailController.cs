namespace HotelChannelManager.Controllers;

using HotelChannelManager.Data;
using HotelChannelManager.Models;
using HotelChannelManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MailController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly FakeMailboxService _fakeMailbox;
    private readonly MailIntegrationService _mailIntegration;

    public MailController(
        AppDbContext context,
        FakeMailboxService fakeMailbox,
        MailIntegrationService mailIntegration)
    {
        _context = context;
        _fakeMailbox = fakeMailbox;
        _mailIntegration = mailIntegration;
    }

    // GET /api/mail → Gelen mailleri listele
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var mails = await _context.IncomingMails
            .OrderByDescending(m => m.ReceivedAt)
            .ToListAsync();

        return Ok(new { success = true, count = mails.Count, data = mails });
    }

    // POST /api/mail/fetch → Fake mailbox'tan mailleri çek
    // Gerçekte: Outlook'tan oku
    [HttpPost("fetch")]
    public async Task<IActionResult> FetchMails()
    {
        var fakeMails = _fakeMailbox.GetFakeMails();
        var added = 0;

        foreach (var mail in fakeMails)
        {
            // Aynı maili tekrar ekleme
            var exists = await _context.IncomingMails
                .AnyAsync(m =>
                    m.From == mail.From &&
                    m.Subject == mail.Subject);

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
            message = $"{added} yeni mail eklendi",
            total = added
        });
    }

    // POST /api/mail/{id}/convert → Maili convert et
    // Admin bu butona basar → Worker PDF'i işler
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
}