using MailSharp.Core.Config;
using MailSharp.Core.Services;
using MailSharp.Infrastructure.Database;
using MailSharp.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MailSharp.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMailService(this IServiceCollection services, Action<SmtpSettings> configureOptions)
    {
        // Cria e configura a instância de SmtpSettings
        var smtpSettings = new SmtpSettings();
        configureOptions.Invoke(smtpSettings);

        // Registra a instância configurada no container
        services.AddSingleton(smtpSettings);

        // Registra o serviço de envio de e-mails
        services.AddScoped<IEmailSender, EmailSender>();

        return services;
    }
    public static IServiceCollection AddMailSharpDatabase(this IServiceCollection services, string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentException("Connection string for MailSharp database not provided.");

        services.AddDbContext<MailSharpDbContext>(options =>
        {
#if DEBUG
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();
#endif
            //if (connectionString.Contains("Filename="))
            //    options.UseSqlite(connectionString);
            //else
            options.UseNpgsql(connectionString);
        });
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
        services.AddScoped<IEmailMessageRepository, EmailMessageRepository>();


        return services;
    }
}
