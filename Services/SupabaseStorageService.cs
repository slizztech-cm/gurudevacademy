using System.Net.Http.Headers;

namespace GurudevDefenceAcademy.Services;

public interface ISupabaseStorageService
{
    bool IsConfigured { get; }
    Task<string?> UploadPdfAsync(Stream content, CancellationToken ct = default);
    Task DeleteAsync(string publicUrl, CancellationToken ct = default);
}

// Uploads study PDFs to a public Supabase Storage bucket and returns the public URL.
// Used so files survive on hosts with an ephemeral disk (e.g. Render free tier).
public class SupabaseStorageService(
    IHttpClientFactory httpFactory,
    IConfiguration config,
    ILogger<SupabaseStorageService> logger) : ISupabaseStorageService
{
    private string? BaseUrl => config["Supabase:Url"]?.TrimEnd('/');
    private string? Key     => config["Supabase:ServiceKey"];
    private string  Bucket  => config["Supabase:Bucket"] is { Length: > 0 } b ? b : "study";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(Key);

    public async Task<string?> UploadPdfAsync(Stream content, CancellationToken ct = default)
    {
        if (!IsConfigured) return null;

        var path = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}.pdf";
        var http = httpFactory.CreateClient();

        // Buffer so we can set Content-Type/Length on a seekable stream.
        var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        ms.Position = 0;

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/storage/v1/object/{Bucket}/{path}");
        req.Headers.TryAddWithoutValidation("apikey", Key);
        req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {Key}");
        req.Content = new StreamContent(ms);
        req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        var resp = await http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            logger.LogError("Supabase upload failed: {Status} {Body}",
                resp.StatusCode, await resp.Content.ReadAsStringAsync(ct));
            return null;
        }
        return $"{BaseUrl}/storage/v1/object/public/{Bucket}/{path}";
    }

    public async Task DeleteAsync(string publicUrl, CancellationToken ct = default)
    {
        if (!IsConfigured || string.IsNullOrEmpty(publicUrl)) return;

        var marker = $"/storage/v1/object/public/{Bucket}/";
        var i = publicUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (i < 0) return;   // not a file we manage in this bucket

        var path = publicUrl[(i + marker.Length)..];
        var http = httpFactory.CreateClient();
        using var req = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/storage/v1/object/{Bucket}/{path}");
        req.Headers.TryAddWithoutValidation("apikey", Key);
        req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {Key}");
        try { await http.SendAsync(req, ct); }
        catch (Exception ex) { logger.LogWarning(ex, "Supabase delete failed for {Url}", publicUrl); }
    }
}
