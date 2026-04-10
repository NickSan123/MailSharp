namespace MailSharp.Core.Models;

public class EmailMessage
{
    public required string To { get; set; }
    public string Subject { get; set; } = default!;
    public required string Body { get; set; } = default!;
    public bool IsHtml { get; set; } = true;
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
