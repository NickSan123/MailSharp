using DotNetEnv;
using MailSharp.Core.Config;
using MailSharp.Core.Extensions;
using MailSharp.Core.Repository;
using MailSharp.Core.Services;
using MailSharp.Worker;

Env.Load();

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // Adicione a configuração do RabbitMQ
        services.AddRabbitMQ(options =>
        {
            options.HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
            options.Port = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672");
            options.UserName = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? "guest";
            options.Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest";
            options.ClientProvidedName = Environment.GetEnvironmentVariable("CLIENT_PROVIDED_NAME") ?? "mailsharp-service";
        });

        // Adicione a configuração do SMTP
        services.AddMailService(options =>
        {
            options.Host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "";
            options.Port = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var port) ? port : 587;
            options.Username = Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? "";
            options.Password = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? "";
            options.SenderName = Environment.GetEnvironmentVariable("SMTP_SENDER_NAME") ?? "Empresa";
            options.SenderEmail = Environment.GetEnvironmentVariable("SMTP_SENDER_EMAIL") ?? "no-reply@minhaempresa.com";
            options.UseStartTls = bool.TryParse(Environment.GetEnvironmentVariable("SMTP_USE_STARTTLS"), out var useTls) && useTls;
        });

        // Adicione o contexto do banco de dados
        var connectionString = Environment.GetEnvironmentVariable("MAILSHARP_DB_CONNECTION");
        services.AddMailSharpDatabase(connectionString);

        // Adicione o Worker como um serviço hospedado
        services.AddHostedService<EmailQueueWorker>();
    });

var host = builder.Build();

// Crie um escopo para resolver serviços antes de iniciar a aplicação
using (var scope = host.Services.CreateScope())
{
    var rabbitMQService = scope.ServiceProvider.GetRequiredService<IRabbitMQService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Garantindo que a fila 'mail_sharp' e sua DLQ existam...");
        // Use o serviço para declarar a fila
        await rabbitMQService.EnsureQueueWithDeadLetterAsync("mail_sharp");
        logger.LogInformation("Fila 'mail_sharp' verificada/criada com sucesso.");
    }
    catch (Exception ex)
    {
        // Se falhar, logue o erro e encerre a aplicação
        logger.LogError(ex, "Ocorreu um erro fatal ao tentar declarar a fila no RabbitMQ. A aplicação será encerrada.");
        return; // Encerra a aplicação, pois o worker não pode funcionar sem a fila
    }
}

// Agora que a infraestrutura está pronta, inicie o host de forma assíncrona
await host.RunAsync();

// REMOVA A LINHA: builder.Build().Run();