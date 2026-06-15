using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GurudevDefenceAcademy.Data;
using GurudevDefenceAcademy.Middleware;
using GurudevDefenceAcademy.Models.Entities;

namespace GurudevDefenceAcademy.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/batches")]
[AdminAuth]
public class BatchesController(AppDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewData["Heading"] = "Batches";
        return View(await db.Batches.Include(b => b.Students).OrderByDescending(b => b.Year).ToListAsync());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Batch model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            TempData["Err"] = "Batch name is required.";
            return RedirectToAction(nameof(Index));
        }
        db.Batches.Add(model);
        await db.SaveChangesAsync();
        TempData["Ok"] = $"Batch “{model.Name}” created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Batch model)
    {
        var b = await db.Batches.FindAsync(model.Id);
        if (b is null) { TempData["Err"] = "Not found."; return RedirectToAction(nameof(Index)); }
        b.Name = model.Name.Trim();
        b.ClassLevel = model.ClassLevel;
        b.Year = model.Year;
        b.Description = model.Description;
        b.IsActive = model.IsActive;
        await db.SaveChangesAsync();
        TempData["Ok"] = "Batch updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var b = await db.Batches.FindAsync(id);
        if (b is not null) { db.Batches.Remove(b); await db.SaveChangesAsync(); }
        TempData["Ok"] = "Batch removed.";
        return RedirectToAction(nameof(Index));
    }
}
