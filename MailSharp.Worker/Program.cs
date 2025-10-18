using MailSharp.Core.Config;
using MailSharp.Core.Services;
using MailSharp.Worker;

var builder = Host.CreateApplicationBuilder(args);
//Env.Load();

// Carrega as configurações do SMTP a partir do .env
var smtpSettings = new SmtpSettings
{
    Host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "",
    Port = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var port) ? port : 587,
    Username = Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? "",
    Password = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? "",
    SenderName = Environment.GetEnvironmentVariable("SMTP_SENDER_NAME") ?? "Online Telecom",
    SenderEmail = Environment.GetEnvironmentVariable("SMTP_SENDER_EMAIL") ?? "no-reply@minhaempresa.com",
    UseStartTls = bool.TryParse(Environment.GetEnvironmentVariable("SMTP_USE_STARTTLS"), out var useTls) && useTls
};

builder.Services.AddSingleton(smtpSettings);
builder.Services.AddScoped<IEmailSender, EmailSender>();


builder.AddServiceDefaults();
builder.Services.AddHostedService<EmailQueueWorker>();

var host = builder.Build();
host.Run();
