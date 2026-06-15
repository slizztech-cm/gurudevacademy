using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using GurudevDefenceAcademy.Models.Entities;

namespace GurudevDefenceAcademy.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<CourseCategory> CourseCategories => Set<CourseCategory>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<YoutubeVideo> YoutubeVideos => Set<YoutubeVideo>();
    public DbSet<ChannelVideo> ChannelVideos => Set<ChannelVideo>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<StudyPdf> StudyPdfs => Set<StudyPdf>();
    public DbSet<JoinRequest> JoinRequests => Set<JoinRequest>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    // Data Protection keys persisted in DB (survives container restarts).
    public DbSet<Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey>
        DataProtectionKeys => Set<Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<AppUser>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.HasOne(u => u.Batch)
             .WithMany(ba => ba.Students)
             .HasForeignKey(u => u.BatchId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<CourseCategory>().HasIndex(c => c.Slug).IsUnique();

        b.Entity<Course>(e =>
        {
            e.HasIndex(c => c.Slug);
            e.HasOne(c => c.Category)
             .WithMany(cat => cat.Courses)
             .HasForeignKey(c => c.CategoryId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Property(c => c.Fees).HasColumnType("numeric(10,2)");
        });

        b.Entity<YoutubeVideo>()
            .HasOne(v => v.Batch)
            .WithMany(ba => ba.Videos)
            .HasForeignKey(v => v.BatchId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<ChatMessage>().HasIndex(m => new { m.BatchId, m.SentAt });

        b.Entity<ChannelVideo>(e =>
        {
            e.HasIndex(v => v.YoutubeId).IsUnique();
            e.HasIndex(v => v.Topic);
        });

        b.Entity<StudyPdf>(e =>
        {
            e.Property(p => p.Price).HasColumnType("numeric(10,2)");
            e.HasOne(p => p.Category)
             .WithMany()
             .HasForeignKey(p => p.CategoryId)
             .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
