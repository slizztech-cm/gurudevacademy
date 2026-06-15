using Microsoft.AspNetCore.Mvc;
using GurudevDefenceAcademy.Middleware;
using GurudevDefenceAcademy.Models.Entities;
using GurudevDefenceAcademy.Services;

namespace GurudevDefenceAcademy.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/courses")]
[AdminAuth]
public class CoursesController(ICourseService courseService) : Controller
{
    private static string Slugify(string s) =>
        new string(s.ToLower().Trim().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray())
            .Replace("--", "-").Trim('-');

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewData["Heading"] = "Courses";
        ViewBag.Categories = await courseService.GetCategoriesAsync(activeOnly: false);
        return View(await courseService.GetCoursesAsync());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Course model)
    {
        if (string.IsNullOrWhiteSpace(model.Name) || model.CategoryId == 0)
        {
            TempData["Err"] = "Course name and category are required.";
            return RedirectToAction(nameof(Index));
        }
        model.Slug = Slugify(string.IsNullOrWhiteSpace(model.Slug) ? model.Name : model.Slug);
        await courseService.CreateCourseAsync(model);
        TempData["Ok"] = $"Course “{model.Name}” added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Course model)
    {
        var c = await courseService.GetCourseAsync(model.Id);
        if (c is null) { TempData["Err"] = "Not found."; return RedirectToAction(nameof(Index)); }
        c.Name = model.Name.Trim();
        c.CategoryId = model.CategoryId;
        c.Icon = string.IsNullOrWhiteSpace(model.Icon) ? "🎯" : model.Icon;
        c.Description = model.Description;
        c.DurationText = model.DurationText;
        c.Fees = model.Fees;
        c.DisplayOrder = model.DisplayOrder;
        c.IsActive = model.IsActive;
        await courseService.UpdateCourseAsync(c);
        TempData["Ok"] = "Course updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await courseService.DeleteCourseAsync(id);
        TempData["Ok"] = "Course removed.";
        return RedirectToAction(nameof(Index));
    }
}
