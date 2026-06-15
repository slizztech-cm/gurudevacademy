using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GurudevDefenceAcademy.Data;
using GurudevDefenceAcademy.Middleware;
using GurudevDefenceAcademy.Models.Entities;

namespace GurudevDefenceAcademy.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/videos")]
[AdminAuth]
public class VideosController(AppDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(int? batchId)
    {
        ViewData["Heading"] = "Class Videos";
        ViewBag.Batches = await db.Batches.OrderBy(b => b.Name).ToListAsync();
        ViewBag.SelectedBatch = batchId;
        var q = db.YoutubeVideos.Include(v => v.Batch).AsQueryable();
        if (batchId is not null) q = q.Where(v => v.BatchId == batchId);
        return View(await q.OrderBy(v => v.DisplayOrder).ToListAsync());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(YoutubeVideo model)
    {
        if (string.IsNullOrWhiteSpace(model.Title) || model.BatchId == 0)
        {
            TempData["Err"] = "Title and batch are required.";
            return RedirectToAction(nameof(Index));
        }
        db.YoutubeVideos.Add(model);
        await db.SaveChangesAsync();
        TempData["Ok"] = "Video added.";
        return RedirectToAction(nameof(Index), new { batchId = model.BatchId });
    }

    [HttpPost("update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(YoutubeVideo model)
    {
        var v = await db.YoutubeVideos.FindAsync(model.Id);
        if (v is null) { TempData["Err"] = "Video not found."; return RedirectToAction(nameof(Index)); }
        if (string.IsNullOrWhiteSpace(model.Title) || model.BatchId == 0)
        {
            TempData["Err"] = "Title and batch are required.";
            return RedirectToAction(nameof(Index), new { batchId = v.BatchId });
        }
        v.BatchId      = model.BatchId;
        v.Title        = model.Title.Trim();
        v.YoutubeRef   = model.YoutubeRef.Trim();
        v.Subject      = string.IsNullOrWhiteSpace(model.Subject) ? null : model.Subject.Trim();
        v.Description  = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        v.DisplayOrder = model.DisplayOrder;
        await db.SaveChangesAsync();
        TempData["Ok"] = "Video updated.";
        return RedirectToAction(nameof(Index), new { batchId = v.BatchId });
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var v = await db.YoutubeVideos.FindAsync(id);
        var bid = v?.BatchId;
        if (v is not null) { db.YoutubeVideos.Remove(v); await db.SaveChangesAsync(); }
        TempData["Ok"] = "Video removed.";
        return RedirectToAction(nameof(Index), new { batchId = bid });
    }
}
