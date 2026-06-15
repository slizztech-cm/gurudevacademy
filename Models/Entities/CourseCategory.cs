using System.ComponentModel.DataAnnotations;

namespace GurudevDefenceAcademy.Models.Entities;

public class CourseCategory
{
    public int Id { get; set; }

    [MaxLength(120)]
    public string Name { get; set; } = "";

    [MaxLength(140)]
    public string Slug { get; set; } = "";

    [MaxLength(20)]
    public string Icon { get; set; } = "📚";

    [MaxLength(600)]
    public string? Description { get; set; }

    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Course> Courses { get; set; } = new();
}
