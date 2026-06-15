using System.ComponentModel.DataAnnotations;

namespace GurudevDefenceAcademy.Models.Entities;

// A Batch is a group of students (a classroom). Students of the same batch
// can chat together and see the same set of YouTube class videos.
public class Batch
{
    public int Id { get; set; }

    [MaxLength(120)]
    public string Name { get; set; } = "";       // e.g. "Agniveer 2026 Morning"

    [MaxLength(40)]
    public string ClassLevel { get; set; } = "";  // e.g. "Class 11", "Agniveer"

    public int Year { get; set; } = DateTime.UtcNow.Year;

    [MaxLength(400)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<AppUser> Students { get; set; } = new();
    public List<YoutubeVideo> Videos { get; set; } = new();
}
