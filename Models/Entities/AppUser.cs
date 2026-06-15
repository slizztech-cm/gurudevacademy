using System.ComponentModel.DataAnnotations;

namespace GurudevDefenceAcademy.Models.Entities;

public class AppUser
{
    public int Id { get; set; }

    [MaxLength(120)]
    public string Name { get; set; } = "";

    [MaxLength(160)]
    public string Email { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    // user | admin | superadmin
    [MaxLength(20)]
    public string Role { get; set; } = "user";

    // Student-specific: which batch / class the student belongs to
    public int? BatchId { get; set; }
    public Batch? Batch { get; set; }

    [MaxLength(40)]
    public string? ClassLevel { get; set; }   // e.g. "Class 11", "Agniveer", "Airforce"

    [MaxLength(20)]
    public string? Phone { get; set; }

    public bool IsActive { get; set; } = true;
    public bool EmailVerified { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}
