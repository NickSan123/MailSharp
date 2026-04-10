using MailSharp.Core.Models;
using System.Security.Cryptography;
using System.Text;

namespace MailSharp.Core.Utils;

public static class KeyEncryptHelpers
{
    public static string GenerateKey(EmailMessage message)
    {
        var raw = $"{message.To}|{message.Subject}|{message.Body}";

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(raw));

        return Convert.ToHexString(hash);
    }
}