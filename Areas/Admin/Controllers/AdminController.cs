using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GurudevDefenceAcademy.Data;
using GurudevDefenceAcademy.Middleware;
using GurudevDefenceAcademy.Models.ViewModels;
using GurudevDefenceAcademy.Services;

namespace GurudevDefenceAcademy.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin")]
public class AdminController(IUserService userService, AppDbContext db) : Controller
{
    [HttpGet("login")]
    public IActionResult Login()
    {
        if (HttpContext.Session.GetString("admin_role") is "admin" or "superadmin")
            return Redirect("/admin/dashboard");
        return View(new LoginViewModel());
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await userService.AuthenticateAsync(model.Email, model.Password);
        if (user is null || user.Role is not ("admin" or "superadmin"))
        {
            ModelState.AddModelError(string.Empty, "Invalid admin credentials.");
            return View(model);
        }

        HttpContext.Session.SetString("admin_email", user.Email);
        HttpContext.Session.SetString("admin_name", user.Name);
        HttpContext.Session.SetString("admin_role", user.Role);
        return Redirect("/admin/dashboard");
    }

    [HttpGet("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove("admin_email");
        HttpContext.Session.Remove("admin_name");
        HttpContext.Session.Remove("admin_role");
        return Redirect("/admin/login");
    }

    [HttpGet("")]
    [HttpGet("dashboard")]
    [AdminAuth]
    public async Task<IActionResult> Dashboard()
    {
        ViewData["Heading"] = "Dashboard";
        ViewBag.Categories = await db.CourseCategories.CountAsync();
        ViewBag.Courses    = await db.Courses.CountAsync();
        ViewBag.Students   = await db.Users.CountAsync(u => u.Role == "user");
        ViewBag.Batches    = await db.Batches.CountAsync();
        ViewBag.NewJoins   = await db.JoinRequests.CountAsync(j => j.Status == "new");
        ViewBag.Unread     = await db.ContactMessages.CountAsync(m => !m.IsRead);
        ViewBag.RecentJoins = await db.JoinRequests.OrderByDescending(j => j.CreatedAt).Take(6).ToListAsync();
        return View();
    }
}
