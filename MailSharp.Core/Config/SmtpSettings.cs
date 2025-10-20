using System;
using System.Collections.Generic;
using System.Text;

namespace MailSharp.Core.Config;

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SenderName { get; set; } = "Empresa";
    public string SenderEmail { get; set; } = "no-reply@minhaempresa.com";
    public bool UseStartTls { get; set; } = true;
}
