using MailSharp.Core.Models;

namespace MailSharp.Core.Services;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message);
}
