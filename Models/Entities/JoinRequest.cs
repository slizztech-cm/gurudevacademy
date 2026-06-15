using System.ComponentModel.DataAnnotations;

namespace GurudevDefenceAcademy.Models.Entities;

// Admission enquiry submitted from the "Join Course" form on the public site.
public class JoinRequest
{
    public int Id { get; set; }

    [MaxLength(120)]
    public string Name { get; set; } = "";

    [MaxLength(20)]
    public string Phone { get; set; } = "";

    [MaxLength(160)]
    public string? Email { get; set; }

    [MaxLength(160)]
    public string? CourseName { get; set; }

    [MaxLength(1000)]
    public string? Message { get; set; }

    // new | contacted | enrolled | closed
    [MaxLength(20)]
    public string Status { get; set; } = "new";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
