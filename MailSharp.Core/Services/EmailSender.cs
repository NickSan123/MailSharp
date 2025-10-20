using MailKit;
using MailKit.Net.Smtp;
using MailSharp.Core.Config;
using MailSharp.Core.Models;
using MimeKit;

namespace MailSharp.Core.Services
{
    public class EmailSender(SmtpSettings smtpSettings) : IEmailSender
    {
        private readonly SmtpSettings _smtpSettings = smtpSettings;

        public async Task SendAsync(EmailMessage message)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_smtpSettings.SenderName, _smtpSettings.SenderEmail));
            email.To.Add(MailboxAddress.Parse(message.To));
            email.Subject = message.Subject;

            email.Body = new TextPart(message.IsHtml ? "html" : "plain")
            {
                Text = message.Body
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port,
                _smtpSettings.UseStartTls ? MailKit.Security.SecureSocketOptions.StartTls
                                          : MailKit.Security.SecureSocketOptions.Auto);
            await smtp.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
