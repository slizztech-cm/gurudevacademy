using Microsoft.AspNetCore.Mvc;
using GurudevDefenceAcademy.Middleware;
using GurudevDefenceAcademy.Models.Entities;
using GurudevDefenceAcademy.Services;

namespace GurudevDefenceAcademy.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/categories")]
[AdminAuth]
public class CategoriesController(ICourseService courseService) : Controller
{
    private static string Slugify(string s) =>
        new string(s.ToLower().Trim().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray())
            .Replace("--", "-").Trim('-');

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewData["Heading"] = "Course Categories";
        return View(await courseService.GetCategoriesAsync(activeOnly: false));
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CourseCategory model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            TempData["Err"] = "Category name is required.";
            return RedirectToAction(nameof(Index));
        }
        model.Slug = string.IsNullOrWhiteSpace(model.Slug) ? Slugify(model.Name) : Slugify(model.Slug);
        await courseService.CreateCategoryAsync(model);
        TempData["Ok"] = $"Category “{model.Name}” added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(CourseCategory model)
    {
        var cat = await courseService.GetCategoryAsync(model.Id);
        if (cat is null) { TempData["Err"] = "Not found."; return RedirectToAction(nameof(Index)); }
        cat.Name = model.Name.Trim();
        cat.Icon = string.IsNullOrWhiteSpace(model.Icon) ? "📚" : model.Icon;
        cat.Description = model.Description;
        cat.DisplayOrder = model.DisplayOrder;
        cat.IsActive = model.IsActive;
        await courseService.UpdateCategoryAsync(cat);
        TempData["Ok"] = "Category updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await courseService.DeleteCategoryAsync(id);
        TempData["Ok"] = "Category removed.";
        return RedirectToAction(nameof(Index));
    }
}
