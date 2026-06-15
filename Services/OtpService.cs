using Microsoft.Extensions.Caching.Memory;

namespace GurudevDefenceAcademy.Services;

public interface IOtpService
{
    string Generate(string email);
    bool Verify(string email, string code);
}

// 6-digit OTP held in memory for 10 minutes. Used for email login + signup verify.
public class OtpService(IMemoryCache cache) : IOtpService
{
    private static string Key(string email) => $"otp:{email.Trim().ToLower()}";

    public string Generate(string email)
    {
        var code = Random.Shared.Next(100000, 999999).ToString();
        cache.Set(Key(email), code, TimeSpan.FromMinutes(10));
        return code;
    }

    public bool Verify(string email, string code)
    {
        if (cache.TryGetValue(Key(email), out string? stored) && stored == code?.Trim())
        {
            cache.Remove(Key(email));
            return true;
        }
        return false;
    }
}
