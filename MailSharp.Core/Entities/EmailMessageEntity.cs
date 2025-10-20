using System;
using System.Collections.Generic;
using System.Text;

namespace MailSharp.Core.Entities;

public class EmailMessageEntity
{
    public int Id { get; set; }
    public string To { get; set; } = string.Empty;
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool SentSuccessfully { get; set; }
    public string? ErrorMessage { get; set; }
}
