using Microsoft.EntityFrameworkCore;
using GurudevDefenceAcademy.Models.Entities;

namespace GurudevDefenceAcademy.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IConfiguration config, ILogger logger)
    {
        await db.Database.MigrateAsync();

        // ---- Admin user ----
        var adminEmail = (config["Admin:Email"] ?? "admin@gurudevdefence.in").ToLower();
        if (!await db.Users.AnyAsync(u => u.Email == adminEmail))
        {
            db.Users.Add(new AppUser
            {
                Name          = config["Admin:Name"] ?? "Academy Admin",
                Email         = adminEmail,
                PasswordHash  = BCrypt.Net.BCrypt.HashPassword(config["Admin:Password"] ?? "admin123"),
                Role          = "superadmin",
                EmailVerified = true,
                IsActive      = true
            });
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded admin user {Email}", adminEmail);
        }

        // ---- Extra dev/test accounts (admin + student) ----
        await EnsureUserAsync(db, logger, "admin@gmail.com", "Admin@123", "Admin",  "admin");
        await EnsureUserAsync(db, logger, "user@gmail.com",  "user@123",  "Student", "user");

        // ---- Course categories + courses ----
        if (!await db.CourseCategories.AnyAsync())
        {
            var defence = new CourseCategory
            {
                Name = "Defence Exams", Slug = "defence", Icon = "🎖️", DisplayOrder = 1,
                Description = "Agniveer, Airforce and other armed-forces entrance preparation.",
                Courses = new()
                {
                    new Course { Name = "Agniveer (Army)", Slug = "agniveer-army", Icon = "🪖", DurationText = "6 months", Description = "Complete written + physical prep for the Agniveer Army entrance.", DisplayOrder = 1 },
                    new Course { Name = "Indian Airforce (Agniveer Vayu)", Slug = "airforce", Icon = "✈️", DurationText = "6–8 months", Description = "Science & English track for Airforce Group X & Y / Agniveer Vayu.", DisplayOrder = 2 },
                    new Course { Name = "Navy / Other Defence", Slug = "navy-defence", Icon = "⚓", DurationText = "6 months", Description = "Foundation for Navy SSR/MR and allied defence exams.", DisplayOrder = 3 },
                }
            };
            var school = new CourseCategory
            {
                Name = "School (Class 9–12)", Slug = "school", Icon = "📐", DisplayOrder = 2,
                Description = "Physics, Chemistry, Maths and English for classes 9 to 12.",
                Courses = new()
                {
                    new Course { Name = "Class 9 & 10 (Science + Maths)", Slug = "class-9-10", Icon = "📘", DurationText = "1 year", Description = "Strong board foundation in Science, Maths and English.", DisplayOrder = 1 },
                    new Course { Name = "Class 11 & 12 (PCM)", Slug = "class-11-12-pcm", Icon = "🧪", DurationText = "1–2 years", Description = "Physics, Chemistry & Maths for board + competitive exams.", DisplayOrder = 2 },
                    new Course { Name = "English (Class 9–12)", Slug = "english", Icon = "📖", DurationText = "1 year", Description = "Grammar, comprehension and writing skills.", DisplayOrder = 3 },
                }
            };
            db.CourseCategories.AddRange(defence, school);
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded course categories and courses.");
        }

        // ---- A starter batch + sample class videos ----
        if (!await db.Batches.AnyAsync())
        {
            var batch = new Batch
            {
                Name = "Foundation Batch 2026", ClassLevel = "Agniveer", Year = 2026,
                Description = "Default batch — assign students here from the admin panel.",
                Videos = new()
                {
                    new YoutubeVideo { Title = "Welcome & Orientation", YoutubeRef = "https://www.youtube.com/@baljeetsir723/videos", Subject = "General", Description = "Introductory class.", DisplayOrder = 1 },
                }
            };
            db.Batches.Add(batch);
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded starter batch.");
        }
    }

    // Creates the account if its email isn't already present (idempotent).
    private static async Task EnsureUserAsync(
        AppDbContext db, ILogger logger, string email, string password, string name, string role)
    {
        email = email.ToLower();
        if (await db.Users.AnyAsync(u => u.Email == email)) return;

        db.Users.Add(new AppUser
        {
            Name          = name,
            Email         = email,
            PasswordHash  = BCrypt.Net.BCrypt.HashPassword(password),
            Role          = role,
            EmailVerified = true,
            IsActive      = true
        });
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Role} account {Email}", role, email);
    }
}
