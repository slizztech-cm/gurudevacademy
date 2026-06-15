using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using GurudevDefenceAcademy.Data;
using GurudevDefenceAcademy.Models.Entities;

namespace GurudevDefenceAcademy.Services;

public record SyncResult(bool Ok, int Added, int Updated, int Total, string? Error);

public interface IYoutubeChannelService
{
    bool IsConfigured { get; }
    Task<SyncResult> SyncAsync(CancellationToken ct = default);
    string DetectTopic(string title);
}

public static class YoutubeUrl
{
    // Pulls the 11-char video id out of any YouTube URL (watch / youtu.be /
    // embed / shorts) or returns the input if it's already a bare id.
    public static string? ExtractVideoId(string urlOrId)
    {
        if (string.IsNullOrWhiteSpace(urlOrId)) return null;
        var r = urlOrId.Trim();

        string? Take(string marker)
        {
            var i = r.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (i < 0) return null;
            var rest = r[(i + marker.Length)..];
            var id = rest.Split('&', '?', '/')[0];
            return id.Length is >= 8 and <= 20 ? id : null;
        }

        var fromUrl = Take("watch?v=") ?? Take("youtu.be/") ?? Take("/embed/") ?? Take("/shorts/") ?? Take("/live/");
        if (fromUrl is not null) return fromUrl;

        // bare id (no slashes, no "@" channel handle)
        if (!r.Contains('/') && !r.Contains('@') && r.Length is >= 8 and <= 20) return r;
        return null;
    }
}

// Pulls the academy's YouTube channel uploads via the YouTube Data API v3 and
// upserts them into the ChannelVideos table. Topic is auto-detected from the
// title; an admin's manual Topic edit (TopicLocked) is preserved across syncs.
public class YoutubeChannelService(
    AppDbContext db,
    IHttpClientFactory httpFactory,
    IConfiguration config,
    ILogger<YoutubeChannelService> logger) : IYoutubeChannelService
{
    private const string Api = "https://www.googleapis.com/youtube/v3";

    private string? ApiKey => config["Youtube:ApiKey"];
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    public async Task<SyncResult> SyncAsync(CancellationToken ct = default)
    {
        if (!IsConfigured)
            return new SyncResult(false, 0, 0, 0, "YouTube API key not configured (Youtube:ApiKey in appsettings.json).");

        var http = httpFactory.CreateClient("youtube");
        try
        {
            var uploadsPlaylist = await ResolveUploadsPlaylistAsync(http, ct);
            if (uploadsPlaylist is null)
                return new SyncResult(false, 0, 0, 0, "Could not resolve the channel's uploads playlist. Check the handle/channel id and API key.");

            var fetched = await FetchPlaylistItemsAsync(http, uploadsPlaylist, ct);

            var existing = await db.ChannelVideos.ToDictionaryAsync(v => v.YoutubeId, ct);
            int added = 0, updated = 0;

            foreach (var item in fetched)
            {
                if (existing.TryGetValue(item.VideoId, out var row))
                {
                    row.Title        = item.Title;
                    row.Description  = item.Description;
                    row.ThumbnailUrl = item.Thumbnail;
                    row.PublishedAt  = item.PublishedAt;
                    if (!row.TopicLocked) row.Topic = DetectTopic(item.Title);
                    row.SyncedAt     = DateTime.UtcNow;
                    updated++;
                }
                else
                {
                    db.ChannelVideos.Add(new ChannelVideo
                    {
                        YoutubeId    = item.VideoId,
                        Title        = item.Title,
                        Description  = item.Description,
                        ThumbnailUrl = item.Thumbnail,
                        Topic        = DetectTopic(item.Title),
                        PublishedAt  = item.PublishedAt,
                        IsActive     = true,
                        SyncedAt     = DateTime.UtcNow
                    });
                    added++;
                }
            }

            await db.SaveChangesAsync(ct);
            logger.LogInformation("YouTube sync complete: {Added} added, {Updated} updated.", added, updated);
            return new SyncResult(true, added, updated, fetched.Count, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "YouTube channel sync failed.");
            return new SyncResult(false, 0, 0, 0, ex.Message);
        }
    }

    // ---- API plumbing ----

    private async Task<string?> ResolveUploadsPlaylistAsync(HttpClient http, CancellationToken ct)
    {
        var channelId = config["Youtube:ChannelId"];
        var handle    = config["Youtube:ChannelHandle"];

        string url;
        if (!string.IsNullOrWhiteSpace(channelId))
            url = $"{Api}/channels?part=contentDetails&id={Uri.EscapeDataString(channelId)}&key={ApiKey}";
        else if (!string.IsNullOrWhiteSpace(handle))
            url = $"{Api}/channels?part=contentDetails&forHandle={Uri.EscapeDataString(handle.TrimStart('@'))}&key={ApiKey}";
        else
            return null;

        using var doc = await GetJsonAsync(http, url, ct);
        var items = doc.RootElement.GetProperty("items");
        if (items.GetArrayLength() == 0) return null;

        return items[0].GetProperty("contentDetails")
                       .GetProperty("relatedPlaylists")
                       .GetProperty("uploads").GetString();
    }

    private async Task<List<VideoItem>> FetchPlaylistItemsAsync(HttpClient http, string playlistId, CancellationToken ct)
    {
        var result = new List<VideoItem>();
        string? pageToken = null;

        do
        {
            var url = $"{Api}/playlistItems?part=snippet,contentDetails&maxResults=50&playlistId={playlistId}&key={ApiKey}";
            if (pageToken is not null) url += $"&pageToken={pageToken}";

            using var doc = await GetJsonAsync(http, url, ct);
            var root = doc.RootElement;

            foreach (var item in root.GetProperty("items").EnumerateArray())
            {
                var snippet = item.GetProperty("snippet");
                var title = snippet.GetProperty("title").GetString() ?? "";

                // Skip private/deleted placeholders.
                if (title is "Private video" or "Deleted video") continue;

                var videoId = snippet.TryGetProperty("resourceId", out var rid) && rid.TryGetProperty("videoId", out var vid)
                    ? vid.GetString()
                    : null;
                if (string.IsNullOrEmpty(videoId)) continue;

                var description = snippet.TryGetProperty("description", out var d) ? d.GetString() : null;
                var publishedAt = item.TryGetProperty("contentDetails", out var cd) && cd.TryGetProperty("videoPublishedAt", out var vpa)
                    ? vpa.GetDateTime()
                    : snippet.GetProperty("publishedAt").GetDateTime();

                result.Add(new VideoItem(
                    videoId,
                    title.Length > 300 ? title[..300] : title,
                    Trim(description, 5000),
                    PickThumbnail(snippet),
                    publishedAt.ToUniversalTime()));
            }

            pageToken = root.TryGetProperty("nextPageToken", out var nt) ? nt.GetString() : null;
        }
        while (!string.IsNullOrEmpty(pageToken));

        return result;
    }

    private static async Task<JsonDocument> GetJsonAsync(HttpClient http, string url, CancellationToken ct)
    {
        using var resp = await http.GetAsync(url, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            // Surface the API's error reason (bad key, quota, etc.).
            string reason = body;
            try
            {
                using var err = JsonDocument.Parse(body);
                if (err.RootElement.TryGetProperty("error", out var e) && e.TryGetProperty("message", out var m))
                    reason = m.GetString() ?? body;
            }
            catch { /* keep raw body */ }
            throw new InvalidOperationException($"YouTube API {(int)resp.StatusCode}: {reason}");
        }
        return JsonDocument.Parse(body);
    }

    private static string? PickThumbnail(JsonElement snippet)
    {
        if (!snippet.TryGetProperty("thumbnails", out var t)) return null;
        foreach (var key in new[] { "medium", "high", "standard", "default", "maxres" })
            if (t.TryGetProperty(key, out var th) && th.TryGetProperty("url", out var u))
                return u.GetString();
        return null;
    }

    private static string? Trim(string? s, int max)
        => s is { Length: > 0 } && s.Length > max ? s[..max] : s;

    private record VideoItem(string VideoId, string Title, string? Description, string? Thumbnail, DateTime PublishedAt);

    // ---- Topic auto-detection ----

    // Ordered: more specific phrases first; the first whole-word match wins.
    private static readonly (string Pattern, string Topic)[] TopicRules =
    {
        (@"one\s*word",                 "One Word Substitution"),
        (@"synonym",                    "Synonyms"),
        (@"antonym",                    "Antonyms"),
        (@"idiom|phrase",               "Idioms & Phrases"),
        (@"spotting\s*error|error",     "Spotting Errors"),
        (@"narration|reported\s*speech","Narration"),
        (@"active.*passive|voice",      "Active/Passive Voice"),
        (@"comprehension",              "Comprehension"),
        (@"vocab",                      "Vocabulary"),
        (@"spelling",                   "Spelling"),
        (@"article",                    "Article"),
        (@"preposition",                "Preposition"),
        (@"conjunction",                "Conjunction"),
        (@"adjective",                  "Adjective"),
        (@"adverb",                     "Adverb"),
        (@"pronoun",                    "Pronoun"),
        (@"noun",                       "Noun"),
        (@"\btense\b",                  "Tense"),
        (@"\bverb\b",                   "Verb"),
        (@"grammar",                    "Grammar"),
        (@"reasoning",                  "Reasoning"),
        (@"\bmaths?\b|mathematics",     "Maths"),
        (@"physics",                    "Physics"),
        (@"chemistry",                  "Chemistry"),
        (@"current\s*affairs",          "Current Affairs"),
        (@"general\s*science|\bgs\b",   "General Science"),
        (@"general\s*knowledge|\bgk\b", "GK"),
        (@"english",                    "English"),
    };

    public string DetectTopic(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "General";
        foreach (var (pattern, topic) in TopicRules)
            if (Regex.IsMatch(title, pattern, RegexOptions.IgnoreCase))
                return topic;
        return "General";
    }
}
