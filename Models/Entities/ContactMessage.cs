using System.ComponentModel.DataAnnotations;

namespace GurudevDefenceAcademy.Models.Entities;

// Message submitted from the public "Contact Us" page.
public class ContactMessage
{
    public int Id { get; set; }

    [MaxLength(120)]
    public string Name { get; set; } = "";

    [MaxLength(160)]
    public string Email { get; set; } = "";

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(160)]
    public string? Subject { get; set; }

    [MaxLength(2000)]
    public string Message { get; set; } = "";

    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
