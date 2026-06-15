using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GurudevDefenceAcademy.Data;
using GurudevDefenceAcademy.Middleware;

namespace GurudevDefenceAcademy.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin")]
[AdminAuth]
public class InboxController(AppDbContext db) : Controller
{
    [HttpGet("joins")]
    public async Task<IActionResult> Joins()
    {
        ViewData["Heading"] = "Join Requests";
        return View(await db.JoinRequests.OrderByDescending(j => j.CreatedAt).ToListAsync());
    }

    [HttpPost("joins/status/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> JoinStatus(int id, string status)
    {
        var j = await db.JoinRequests.FindAsync(id);
        if (j is not null) { j.Status = status; await db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Joins));
    }

    [HttpPost("joins/delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> JoinDelete(int id)
    {
        var j = await db.JoinRequests.FindAsync(id);
        if (j is not null) { db.JoinRequests.Remove(j); await db.SaveChangesAsync(); }
        TempData["Ok"] = "Request deleted.";
        return RedirectToAction(nameof(Joins));
    }

    [HttpGet("messages")]
    public async Task<IActionResult> Messages()
    {
        ViewData["Heading"] = "Contact Messages";
        var msgs = await db.ContactMessages.OrderByDescending(m => m.CreatedAt).ToListAsync();
        // mark all as read when viewed
        var unread = msgs.Where(m => !m.IsRead).ToList();
        if (unread.Count > 0)
        {
            unread.ForEach(m => m.IsRead = true);
            await db.SaveChangesAsync();
        }
        return View(msgs);
    }

    [HttpPost("messages/delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MessageDelete(int id)
    {
        var m = await db.ContactMessages.FindAsync(id);
        if (m is not null) { db.ContactMessages.Remove(m); await db.SaveChangesAsync(); }
        TempData["Ok"] = "Message deleted.";
        return RedirectToAction(nameof(Messages));
    }

    [HttpGet("students")]
    public async Task<IActionResult> Students()
    {
        ViewData["Heading"] = "Students";
        ViewBag.Batches = await db.Batches.OrderBy(b => b.Name).ToListAsync();
        return View(await db.Users.Include(u => u.Batch)
            .Where(u => u.Role == "user").OrderByDescending(u => u.CreatedAt).ToListAsync());
    }

    [HttpPost("students/assign/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignBatch(int id, int? batchId)
    {
        var u = await db.Users.FindAsync(id);
        if (u is not null) { u.BatchId = batchId; await db.SaveChangesAsync(); }
        TempData["Ok"] = "Student batch updated.";
        return RedirectToAction(nameof(Students));
    }

    [HttpPost("students/toggle/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var u = await db.Users.FindAsync(id);
        if (u is not null) { u.IsActive = !u.IsActive; await db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Students));
    }
}
