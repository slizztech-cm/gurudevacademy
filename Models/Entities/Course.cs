using System.ComponentModel.DataAnnotations;

namespace GurudevDefenceAcademy.Models.Entities;

public class Course
{
    public int Id { get; set; }

    public int CategoryId { get; set; }
    public CourseCategory? Category { get; set; }

    [MaxLength(160)]
    public string Name { get; set; } = "";

    [MaxLength(180)]
    public string Slug { get; set; } = "";

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(20)]
    public string Icon { get; set; } = "🎯";

    [MaxLength(80)]
    public string? DurationText { get; set; }   // e.g. "6 months", "1 year"

    public decimal? Fees { get; set; }

    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
