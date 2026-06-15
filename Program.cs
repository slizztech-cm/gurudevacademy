using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using GurudevDefenceAcademy.Data;
using GurudevDefenceAcademy.Models.Entities;
using GurudevDefenceAcademy.Repositories.Base;
using GurudevDefenceAcademy.Services;
using GurudevDefenceAcademy.Services.Base;

var builder = WebApplication.CreateBuilder(args);

// Local secrets (gitignored). On Render these come from environment variables
// instead (e.g. ConnectionStrings__DefaultConnection, Smtp__User, Youtube__ApiKey),
// which ASP.NET Core loads automatically and which override the JSON below.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Render (and most PaaS hosts) tell the app which port to listen on via $PORT.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddControllersWithViews();

// ---- Database ----
// Neon serverless Postgres auto-suspends when idle; the first query after a
// cold start can drop the connection ("forcibly closed by the remote host").
// EnableRetryOnFailure transparently retries these transient failures while
// the database wakes back up.
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null)));

// ---- Data Protection (keys persisted in DB) ----
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<AppDbContext>()
    .SetApplicationName("GurudevDefenceAcademy");

// ---- Session ----
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromHours(3);
    o.Cookie.HttpOnly     = true;
    o.Cookie.IsEssential  = true;
    // SameAsRequest: gets the Secure flag on HTTPS (production) but still
    // round-trips on plain HTTP for local development.
    o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    o.Cookie.SameSite     = SameSiteMode.Lax;
});

builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

// ---- Generic base repository/service ----
builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
builder.Services.AddScoped(typeof(IBaseService<>), typeof(BaseService<>));

// ---- Concrete services ----
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddSingleton<IOtpService, OtpService>();

// ---- YouTube channel sync ----
builder.Services.AddHttpClient("youtube");
builder.Services.AddScoped<IYoutubeChannelService, YoutubeChannelService>();

var app = builder.Build();

// Trust proxy headers (https behind a reverse proxy / host).
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ---- Security headers ----
app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"]        = "SAMEORIGIN";
    h["Referrer-Policy"]        = "strict-origin-when-cross-origin";
    h["Permissions-Policy"]     = "geolocation=(), camera=(), microphone=()";
    h["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "img-src 'self' data: https:; " +
        "frame-src https://www.youtube.com https://www.youtube-nocookie.com; " +
        "connect-src 'self';";
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

// ---- Routes ----
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ---- Migrate + seed ----
using (var scope = app.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await DbSeeder.SeedAsync(db, app.Configuration, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database seed/migrate failed at startup.");
    }
}

app.Run();
