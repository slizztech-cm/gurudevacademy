using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GurudevDefenceAcademy.Data;
using GurudevDefenceAcademy.Middleware;
using GurudevDefenceAcademy.Models.Entities;
using GurudevDefenceAcademy.Models.ViewModels;
using GurudevDefenceAcademy.Services;

namespace GurudevDefenceAcademy.Areas.User.Controllers;

[Area("User")]
[Route("student")]
[UserAuth]
public class StudentController(
    AppDbContext db,
    IUserService userService) : Controller
{
    private int Uid => HttpContext.Session.GetInt32("user_id") ?? 0;

    private async Task<AppUser?> CurrentAsync() => await db.Users.Include(u => u.Batch).FirstOrDefaultAsync(u => u.Id == Uid);

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var user = await CurrentAsync();
        ViewBag.User = user;
        ViewBag.VideoCount = user?.BatchId is null ? 0
            : await db.YoutubeVideos.CountAsync(v => v.BatchId == user.BatchId && v.IsActive);
        ViewBag.PdfCount = await db.StudyPdfs.CountAsync(p => p.IsActive);
        return View();
    }

    [HttpGet("classroom")]
    public async Task<IActionResult> Classroom()
    {
        var user = await CurrentAsync();
        ViewBag.User = user;
        if (user?.BatchId is null)
        {
            ViewBag.Videos = new List<YoutubeVideo>();
            return View();
        }
        ViewBag.Videos = await db.YoutubeVideos
            .Where(v => v.BatchId == user.BatchId && v.IsActive)
            .OrderBy(v => v.DisplayOrder).ToListAsync();
        return View();
    }

    [HttpGet("videos")]
    public async Task<IActionResult> Videos(int? batchId)
    {
        ViewData["Title"] = "Video Lectures — Student Portal";

        // Every batch's class videos in one place — students can browse and filter
        // by batch (Classroom stays personal to their own batch + chat).
        var active = db.YoutubeVideos.Include(v => v.Batch).Where(v => v.IsActive);

        // Batch filter chips (with counts). Count per batch in SQL, then attach names in memory.
        var counts = await db.YoutubeVideos
            .Where(v => v.IsActive)
            .GroupBy(v => v.BatchId)
            .Select(g => new { BatchId = g.Key, Count = g.Count() })
            .ToListAsync();
        var batchNames = await db.Batches
            .Where(b => counts.Select(c => c.BatchId).Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b.Name);
        ViewBag.Batches = counts
            .Select(c => new VideoBatchFilter(c.BatchId, batchNames.GetValueOrDefault(c.BatchId, "Batch"), c.Count))
            .OrderBy(x => x.Name)
            .ToList();

        if (batchId is not null) active = active.Where(v => v.BatchId == batchId);

        ViewBag.SelectedBatch = batchId;
        ViewBag.User = await CurrentAsync();

        var videos = await active
            .OrderBy(v => v.Batch!.Name).ThenBy(v => v.DisplayOrder)
            .ToListAsync();
        return View(videos);
    }

    [HttpGet("study-material")]
    public async Task<IActionResult> StudyMaterial()
    {
        ViewData["Title"] = "Study Material — Student Portal";
        var pdfs = await db.StudyPdfs
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.UploadedAt)
            .ToListAsync();
        ViewBag.User = await CurrentAsync();
        return View(pdfs);
    }

    [HttpGet("profile")]
    public async Task<IActionResult> Profile()
    {
        var user = await CurrentAsync();
        if (user is null) return Redirect("/account/login");
        ViewBag.Batches = await db.Batches.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
        return View(new ProfileViewModel
        {
            Name = user.Name, Email = user.Email, Phone = user.Phone,
            BatchId = user.BatchId, ClassLevel = user.ClassLevel
        });
    }

    [HttpPost("profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await CurrentAsync();
        if (user is null) return Redirect("/account/login");

        user.Name       = model.Name.Trim();
        user.Phone      = model.Phone?.Trim();
        user.BatchId    = model.BatchId;
        user.ClassLevel = model.ClassLevel;
        await db.SaveChangesAsync();

        HttpContext.Session.SetString("user_name", user.Name);
        TempData["Ok"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Profile));
    }

    // ---------- Batch chat API (polling) ----------
    [HttpGet("chat/{batchId:int}")]
    public async Task<IActionResult> ChatMessages(int batchId, long since = 0)
    {
        var user = await CurrentAsync();
        if (user?.BatchId != batchId) return Forbid();

        var q = db.ChatMessages.Where(m => m.BatchId == batchId);
        if (since > 0)
        {
            var sinceDt = new DateTime(since, DateTimeKind.Utc);
            q = q.Where(m => m.SentAt > sinceDt);
        }
        var msgs = await q.OrderBy(m => m.SentAt).Take(200).ToListAsync();
        return Ok(msgs.Select(m => new
        {
            id = m.Id, userId = m.UserId, name = m.UserName, text = m.Text,
            at = m.SentAt.ToString("o"), ticks = m.SentAt.Ticks
        }));
    }

    [HttpPost("chat/{batchId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChatSend(int batchId, [FromBody] ChatSendDto dto)
    {
        var user = await CurrentAsync();
        if (user?.BatchId != batchId) return Forbid();

        var text = WebUtility.HtmlEncode((dto.Text ?? "").Trim());
        if (string.IsNullOrEmpty(text) || text.Length > 1000)
            return BadRequest(new { error = "Message must be 1–1000 characters." });

        var msg = new ChatMessage
        {
            BatchId = batchId, UserId = user.Id,
            UserName = user.Name, Text = text, SentAt = DateTime.UtcNow
        };
        db.ChatMessages.Add(msg);
        await db.SaveChangesAsync();
        return Ok(new { id = msg.Id, userId = msg.UserId, name = msg.UserName,
                        text = msg.Text, at = msg.SentAt.ToString("o"), ticks = msg.SentAt.Ticks });
    }

    public class ChatSendDto { public string? Text { get; set; } }
}
