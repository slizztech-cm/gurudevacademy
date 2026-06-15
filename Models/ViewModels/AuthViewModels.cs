using System.ComponentModel.DataAnnotations;

namespace GurudevDefenceAcademy.Models.ViewModels;

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required, MaxLength(120)]
    public string Name { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Phone, MaxLength(20)]
    public string? Phone { get; set; }

    [Required, MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = "";

    [Required, Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = "";
}

public class JoinRequestViewModel
{
    [Required, MaxLength(120)]
    public string Name { get; set; } = "";

    [Required, Phone, MaxLength(20)]
    public string Phone { get; set; } = "";

    [EmailAddress, MaxLength(160)]
    public string? Email { get; set; }

    [MaxLength(160)]
    public string? CourseName { get; set; }

    [MaxLength(1000)]
    public string? Message { get; set; }
}

public class ContactViewModel
{
    [Required, MaxLength(120)]
    public string Name { get; set; } = "";

    [Required, EmailAddress, MaxLength(160)]
    public string Email { get; set; } = "";

    [Phone, MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(160)]
    public string? Subject { get; set; }

    [Required, MaxLength(2000)]
    public string Message { get; set; } = "";
}

public class ProfileViewModel
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public int? BatchId { get; set; }
    public string? ClassLevel { get; set; }
}
