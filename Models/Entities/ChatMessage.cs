using System.ComponentModel.DataAnnotations;

namespace GurudevDefenceAcademy.Models.Entities;

// Batch-scoped classroom chat. Only students of the same batch see these.
public class ChatMessage
{
    public int Id { get; set; }

    public int BatchId { get; set; }
    public Batch? Batch { get; set; }

    public int UserId { get; set; }

    [MaxLength(120)]
    public string UserName { get; set; } = "";

    [MaxLength(1000)]
    public string Text { get; set; } = "";

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
