using GurudevDefenceAcademy.Models.Entities;
using GurudevDefenceAcademy.Repositories.Base;
using GurudevDefenceAcademy.Services.Base;

namespace GurudevDefenceAcademy.Services;

public interface IUserService : IBaseService<AppUser>
{
    Task<AppUser?> AuthenticateAsync(string email, string password);
    Task<AppUser?> GetByEmailAsync(string email);
    Task<AppUser> RegisterAsync(string name, string email, string password, string? phone);
    Task SetPasswordAsync(AppUser user, string newPassword);
    Task TouchLoginAsync(AppUser user);
}

public class UserService(IBaseRepository<AppUser> repo) : BaseService<AppUser>(repo), IUserService
{
    public async Task<AppUser?> GetByEmailAsync(string email)
        => await Repo.GetOneAsync(u => u.Email == email.Trim().ToLower());

    public async Task<AppUser?> AuthenticateAsync(string email, string password)
    {
        var user = await GetByEmailAsync(email);
        if (user is null || !user.IsActive) return null;
        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) ? user : null;
    }

    public async Task<AppUser> RegisterAsync(string name, string email, string password, string? phone)
    {
        var user = new AppUser
        {
            Name          = name.Trim(),
            Email         = email.Trim().ToLower(),
            PasswordHash  = BCrypt.Net.BCrypt.HashPassword(password),
            Phone         = phone?.Trim(),
            Role          = "user",
            EmailVerified = true,   // verified via OTP during signup
            IsActive      = true
        };
        return await Repo.AddAsync(user);
    }

    public async Task SetPasswordAsync(AppUser user, string newPassword)
    {
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await Repo.UpdateAsync(user);
    }

    public async Task TouchLoginAsync(AppUser user)
    {
        user.LastLoginAt = DateTime.UtcNow;
        await Repo.UpdateAsync(user);
    }
}
