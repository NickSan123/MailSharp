using MailSharp.Core.Config;
using MailSharp.Core.Models;
using MailSharp.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MailSharp.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMQ(this IServiceCollection services, Action<RabbitMQOptions> configureOptions)
    {
        services.Configure(configureOptions);

        services.AddSingleton<IRabbitMQService, RabbitMQService>();

        return services;
    }

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

    public static IServiceCollection AddRabbitMQ(this IServiceCollection services, RabbitMQOptions options)
    {
        services.AddSingleton(Options.Create(options));

        services.AddSingleton<IRabbitMQService, RabbitMQService>();

        return services;
    }

    public static IServiceCollection AddRabbitMQ(
        this IServiceCollection services,
        string hostName = "localhost",
        int port = 5672,
        string userName = "guest",
        string password = "guest",
        string clientProvidedName = "mailsharp-service")
    {
        services.Configure<RabbitMQOptions>(options =>
        {
            options.HostName = hostName;
            options.Port = port;
            options.UserName = userName;
            options.Password = password;
            options.ClientProvidedName = clientProvidedName;
        });

        services.AddSingleton<IRabbitMQService, RabbitMQService>();

        return services;
    }
}
