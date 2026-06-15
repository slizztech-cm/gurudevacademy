using System.ComponentModel.DataAnnotations;

namespace GurudevDefenceAcademy.Models.Entities;

// A class video (embedded from YouTube — Baljeet Sir's channel etc.)
// shown inside the Classroom for a particular batch.
public class YoutubeVideo
{
    public int Id { get; set; }

    public int BatchId { get; set; }
    public Batch? Batch { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = "";

    // Just the YouTube video id (e.g. "dQw4w9WgXcQ") OR a full URL — we normalise.
    [MaxLength(400)]
    public string YoutubeRef { get; set; } = "";

    [MaxLength(80)]
    public string? Subject { get; set; }   // Physics / Chemistry / Maths / English

    [MaxLength(600)]
    public string? Description { get; set; }

    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
