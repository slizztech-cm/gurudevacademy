using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using GurudevDefenceAcademy.Models.ViewModels;
using GurudevDefenceAcademy.Services;

namespace GurudevDefenceAcademy.Controllers;

[Route("account")]
public class AccountController(
    IUserService userService,
    IOtpService otpService,
    IEmailService emailService) : Controller
{
    private bool LoggedIn => Context().GetString("user_email") != null;
    private ISession Context() => HttpContext.Session;

    private void SignIn(Models.Entities.AppUser u)
    {
        HttpContext.Session.SetString("user_email", u.Email);
        HttpContext.Session.SetString("user_name", u.Name);
        HttpContext.Session.SetInt32("user_id", u.Id);
    }

    // ---------- LOGIN ----------
    [HttpGet("login")]
    public IActionResult Login(string? returnUrl)
    {
        if (Context().GetString("admin_role") is "admin" or "superadmin")
            return Redirect("/admin/dashboard");
        if (LoggedIn) return Redirect("/student/dashboard");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var existing = await userService.GetByEmailAsync(model.Email);
        if (existing is null)
        {
            ViewBag.ErrorType = "no_account";
            ModelState.AddModelError(string.Empty, "No account found with this email.");
            return View(model);
        }
        var user = await userService.AuthenticateAsync(model.Email, model.Password);
        if (user is null)
        {
            ViewBag.ErrorType = "wrong_password";
            ModelState.AddModelError(string.Empty, "Incorrect password. Please try again.");
            return View(model);
        }

        await userService.TouchLoginAsync(user);

        // Admins logging in via the student form → straight to the admin panel
        // (not the student dashboard), so they see Courses, PDFs, videos, etc.
        if (user.Role is "admin" or "superadmin")
        {
            HttpContext.Session.SetString("admin_email", user.Email);
            HttpContext.Session.SetString("admin_name", user.Name);
            HttpContext.Session.SetString("admin_role", user.Role);
            return Redirect("/admin/dashboard");
        }
        SignIn(user);

        var rt = model.ReturnUrl;
        if (!string.IsNullOrEmpty(rt) && Url.IsLocalUrl(rt)) return Redirect(rt);
        return Redirect("/student/dashboard");
    }

    // ---------- REGISTER (step 1: details → OTP) ----------
    [HttpGet("register")]
    public IActionResult Register()
    {
        if (LoggedIn) return Redirect("/student/dashboard");
        return View(new RegisterViewModel());
    }

    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (await userService.GetByEmailAsync(model.Email) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
            return View(model);
        }

        // Stash pending registration in session, send OTP.
        var pending = JsonSerializer.Serialize(model);
        HttpContext.Session.SetString("pending_reg", pending);

        var otp = otpService.Generate(model.Email);
        try
        {
            await emailService.SendTemplateAsync(model.Email, "Verify your email — Gurudev Defence Academy",
                "Otp.html", new()
                {
                    ["Name"]    = model.Name,
                    ["Otp"]     = otp,
                    ["Purpose"] = "verify your email and finish creating your account"
                });
        }
        catch { /* dev: SMTP may be unset */ }

        TempData["VerifyEmail"] = model.Email;
        return RedirectToAction(nameof(Verify));
    }

    // ---------- REGISTER (step 2: OTP → create) ----------
    [HttpGet("verify")]
    public IActionResult Verify()
    {
        if (HttpContext.Session.GetString("pending_reg") is null) return RedirectToAction(nameof(Register));
        ViewBag.Email = TempData["VerifyEmail"];
        return View();
    }

    [HttpPost("verify")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(string otp)
    {
        var json = HttpContext.Session.GetString("pending_reg");
        if (json is null) return RedirectToAction(nameof(Register));

        var model = JsonSerializer.Deserialize<RegisterViewModel>(json)!;
        if (!otpService.Verify(model.Email, otp))
        {
            ViewBag.Email = model.Email;
            ModelState.AddModelError(string.Empty, "Invalid or expired code. Please try again.");
            return View();
        }

        var user = await userService.RegisterAsync(model.Name, model.Email, model.Password, model.Phone);
        HttpContext.Session.Remove("pending_reg");
        SignIn(user);

        try
        {
            await emailService.SendTemplateAsync(user.Email, "Welcome to Gurudev Defence Academy 🎖️",
                "Welcome.html", new()
                {
                    ["Name"]     = user.Name,
                    ["LoginUrl"] = $"{Request.Scheme}://{Request.Host}/student/dashboard"
                });
        }
        catch { }

        return Redirect("/student/dashboard");
    }

    [HttpGet("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Redirect("/");
    }
}
