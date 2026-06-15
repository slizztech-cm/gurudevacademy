using System.ComponentModel.DataAnnotations;

namespace GurudevDefenceAcademy.Models.Entities;

// Study material PDFs (free or paid) — like SizzleQuiz's store/study section.
public class StudyPdf
{
    public int Id { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = "";

    [MaxLength(800)]
    public string? Description { get; set; }

    public int? CategoryId { get; set; }
    public CourseCategory? Category { get; set; }

    [MaxLength(80)]
    public string? Subject { get; set; }

    // Relative path under wwwroot/uploads or an external URL.
    [MaxLength(500)]
    public string FileUrl { get; set; } = "";

    public bool IsFree { get; set; } = true;
    public decimal? Price { get; set; }

    public int DownloadCount { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
