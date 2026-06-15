using Microsoft.AspNetCore.Mvc;
using GurudevDefenceAcademy.Models.Entities;
using GurudevDefenceAcademy.Models.ViewModels;
using GurudevDefenceAcademy.Repositories.Base;
using GurudevDefenceAcademy.Services;

namespace GurudevDefenceAcademy.Controllers;

public class HomeController(
    ICourseService courseService,
    IBaseRepository<StudyPdf> pdfRepo,
    IBaseRepository<JoinRequest> joinRepo,
    IBaseRepository<ContactMessage> contactRepo,
    IEmailService emailService,
    IConfiguration config) : Controller
{
    [HttpGet("/")]
    public async Task<IActionResult> Index()
    {
        ViewBag.Categories = await courseService.GetCategoriesWithCoursesAsync();
        return View();
    }

    [HttpGet("/courses")]
    public async Task<IActionResult> Courses()
    {
        ViewData["Title"] = "Courses — Gurudev Defence Academy";
        ViewBag.Categories = await courseService.GetCategoriesWithCoursesAsync();
        return View();
    }

    [HttpGet("/study-material")]
    public async Task<IActionResult> StudyMaterial()
    {
        ViewData["Title"] = "Study Material — Gurudev Defence Academy";
        var pdfs = await pdfRepo.GetAllAsync(p => p.IsActive);
        return View(pdfs.OrderByDescending(p => p.UploadedAt).ToList());
    }

    // ---- Join Course ----
    [HttpGet("/join")]
    public async Task<IActionResult> Join(string? course)
    {
        ViewData["Title"] = "Join a Course — Gurudev Defence Academy";
        ViewBag.Courses = await courseService.GetCoursesAsync();
        return View(new JoinRequestViewModel { CourseName = course });
    }

    [HttpPost("/join")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(JoinRequestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Courses = await courseService.GetCoursesAsync();
            return View(model);
        }
        await joinRepo.AddAsync(new JoinRequest
        {
            Name = model.Name.Trim(), Phone = model.Phone.Trim(),
            Email = model.Email?.Trim(), CourseName = model.CourseName,
            Message = model.Message
        });
        TempData["Ok"] = "Thank you! Your enquiry has been received. Our team will contact you soon.";
        return RedirectToAction(nameof(Join));
    }

    // ---- Contact ----
    [HttpGet("/contact")]
    public IActionResult Contact()
    {
        ViewData["Title"] = "Contact Us — Gurudev Defence Academy";
        ViewBag.Address = config["Academy:Address"];
        ViewBag.Email   = config["Academy:Email"];
        ViewBag.Phone   = config["Academy:Phone"];
        return View(new ContactViewModel());
    }

    [HttpPost("/contact")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactViewModel model)
    {
        ViewBag.Address = config["Academy:Address"];
        ViewBag.Email   = config["Academy:Email"];
        ViewBag.Phone   = config["Academy:Phone"];
        if (!ModelState.IsValid) return View(model);

        await contactRepo.AddAsync(new ContactMessage
        {
            Name = model.Name.Trim(), Email = model.Email.Trim(),
            Phone = model.Phone?.Trim(), Subject = model.Subject, Message = model.Message
        });

        var to = config["Academy:Email"];
        if (!string.IsNullOrWhiteSpace(to))
        {
            try
            {
                await emailService.SendAsync(to,
                    $"New contact message: {model.Subject ?? "(no subject)"}",
                    $"<p><b>{model.Name}</b> ({model.Email}, {model.Phone})</p><p>{model.Message}</p>");
            }
            catch { /* email best-effort */ }
        }

        TempData["Ok"] = "Message sent! We'll get back to you shortly.";
        return RedirectToAction(nameof(Contact));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
