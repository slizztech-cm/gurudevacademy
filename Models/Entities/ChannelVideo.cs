using System.ComponentModel.DataAnnotations;

namespace GurudevDefenceAcademy.Models.Entities;

// A video pulled from the academy's YouTube channel (via the YouTube Data API).
// Shown in the student portal "Video Lectures" section, embedded in an iframe and
// filterable by Topic. Topic is auto-detected from the title on sync, but an admin
// can override it (TopicLocked stops sync from overwriting a manual edit).
public class ChannelVideo
{
    public int Id { get; set; }

    // The YouTube video id, e.g. "dQw4w9WgXcQ" — unique per channel.
    [MaxLength(20)]
    public string YoutubeId { get; set; } = "";

    [MaxLength(300)]
    public string Title { get; set; } = "";

    [MaxLength(5000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }

    // Filter bucket, e.g. "Synonyms", "Article", "Adjective", "General".
    [MaxLength(80)]
    public string Topic { get; set; } = "General";

    // When true, a re-sync won't overwrite an admin-chosen Topic.
    public bool TopicLocked { get; set; } = false;

    public DateTime PublishedAt { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
}
