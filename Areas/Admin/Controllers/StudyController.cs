using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GurudevDefenceAcademy.Data;
using GurudevDefenceAcademy.Middleware;
using GurudevDefenceAcademy.Models.Entities;

namespace GurudevDefenceAcademy.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/study")]
[AdminAuth]
public class StudyController(AppDbContext db, IWebHostEnvironment env) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewData["Heading"] = "Study PDFs";
        ViewBag.Categories = await db.CourseCategories.OrderBy(c => c.Name).ToListAsync();
        return View(await db.StudyPdfs.OrderByDescending(p => p.UploadedAt).ToListAsync());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(40_000_000)]
    public async Task<IActionResult> Create(StudyPdf model, IFormFile? file)
    {
        if (string.IsNullOrWhiteSpace(model.Title))
        {
            TempData["Err"] = "Title is required.";
            return RedirectToAction(nameof(Index));
        }

        if (file is { Length: > 0 })
        {
            if (Path.GetExtension(file.FileName).ToLower() != ".pdf")
            {
                TempData["Err"] = "Only PDF files are allowed.";
                return RedirectToAction(nameof(Index));
            }
            var dir = Path.Combine(env.WebRootPath, "uploads");
            Directory.CreateDirectory(dir);
            var name = $"{Guid.NewGuid():N}.pdf";
            await using (var fs = System.IO.File.Create(Path.Combine(dir, name)))
                await file.CopyToAsync(fs);
            model.FileUrl = $"/uploads/{name}";
        }

        if (string.IsNullOrWhiteSpace(model.FileUrl))
        {
            TempData["Err"] = "Upload a PDF file or provide a file URL.";
            return RedirectToAction(nameof(Index));
        }

        db.StudyPdfs.Add(model);
        await db.SaveChangesAsync();
        TempData["Ok"] = "Study material added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("update")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(40_000_000)]
    public async Task<IActionResult> Update(StudyPdf model, IFormFile? file)
    {
        var p = await db.StudyPdfs.FindAsync(model.Id);
        if (p is null) { TempData["Err"] = "Study material not found."; return RedirectToAction(nameof(Index)); }
        if (string.IsNullOrWhiteSpace(model.Title))
        {
            TempData["Err"] = "Title is required.";
            return RedirectToAction(nameof(Index));
        }

        // Optionally replace the PDF: a new upload wins; else an edited external URL.
        if (file is { Length: > 0 })
        {
            if (Path.GetExtension(file.FileName).ToLower() != ".pdf")
            {
                TempData["Err"] = "Only PDF files are allowed.";
                return RedirectToAction(nameof(Index));
            }
            var dir = Path.Combine(env.WebRootPath, "uploads");
            Directory.CreateDirectory(dir);
            var name = $"{Guid.NewGuid():N}.pdf";
            await using (var fs = System.IO.File.Create(Path.Combine(dir, name)))
                await file.CopyToAsync(fs);
            DeleteLocalFile(p.FileUrl);
            p.FileUrl = $"/uploads/{name}";
        }
        else if (!string.IsNullOrWhiteSpace(model.FileUrl) && model.FileUrl.Trim() != p.FileUrl)
        {
            DeleteLocalFile(p.FileUrl);   // switching to a new/external URL
            p.FileUrl = model.FileUrl.Trim();
        }

        p.Title       = model.Title.Trim();
        p.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        p.CategoryId  = model.CategoryId;
        p.Subject     = string.IsNullOrWhiteSpace(model.Subject) ? null : model.Subject.Trim();
        p.IsFree      = model.IsFree;
        p.Price       = model.IsFree ? null : model.Price;
        await db.SaveChangesAsync();
        TempData["Ok"] = "Study material updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await db.StudyPdfs.FindAsync(id);
        if (p is not null)
        {
            DeleteLocalFile(p.FileUrl);
            db.StudyPdfs.Remove(p);
            await db.SaveChangesAsync();
        }
        TempData["Ok"] = "Study material removed.";
        return RedirectToAction(nameof(Index));
    }

    // Removes the backing file if it lives under wwwroot/uploads (external URLs left alone).
    private void DeleteLocalFile(string? fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl) || !fileUrl.StartsWith("/uploads/")) return;
        var path = Path.Combine(env.WebRootPath, fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
    }
}
