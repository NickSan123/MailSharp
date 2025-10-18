using MailSharp.Core.Models;

using MailKit.Net.Smtp;
using MailKit;
using MimeKit;

namespace MailSharp.Core.Services
{
    public class EmailSender : IEmailSender
    {
        public async Task SendAsync(EmailMessage message)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("Online Telecom","no-reply@minhaempresa.com"));
            email.To.Add(MailboxAddress.Parse(message.To));
            email.Subject = message.Subject;

            email.Body = new TextPart(message.IsHtml ? "html" : "plain")
            {
                Text = message.Body
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync("smtp.seuservidor.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync("usuario", "senha");
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
