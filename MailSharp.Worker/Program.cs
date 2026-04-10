using DotNetEnv;
using easy_rabbitmq.Extensions;
using MailSharp.Worker;
using MailSharp.Infrastructure.Extensions;

Env.Load();

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {

        var rabbit_host_name = hostContext.Configuration["rabbit_host_name"];
        var rabbit_port = hostContext.Configuration["rabbit_port"];
        var rabbit_user_name = hostContext.Configuration["rabbit_user_name"];
        var rabbit_password = hostContext.Configuration["rabbit_password"];
        var rabbit_virtual_host = hostContext.Configuration["rabbit_virtual_host"];
        var rabbit_client_provided_name = hostContext.Configuration["rabbit_client_provided_name"];

        int rabbitPort = 5672; 

        if(rabbit_port != null)
        {
            int.TryParse(rabbit_port, out rabbitPort);
        }
        

        services.AddEasyRabbitMQ(options =>
        {
            options.HostName = rabbit_host_name ?? "localhost";
            options.Port = rabbitPort;
            options.UserName = rabbit_user_name ?? "guest";
            options.Password = rabbit_password ?? "guest";
            options.ClientProvidedName = rabbit_client_provided_name ?? "mailsharp-service";
            options.VirtualHost = rabbit_virtual_host ?? "/";
        });

        // Adicione a configuração do SMTP
        services.AddMailService(options =>
        {
            options.Host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "";
            options.Port = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var port) ? port : 587;
            options.Username = Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? "";
            options.Password = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? "#@pass";
            options.SenderName = Environment.GetEnvironmentVariable("SMTP_SENDER_NAME") ?? "Empresa";
            options.SenderEmail = Environment.GetEnvironmentVariable("SMTP_SENDER_EMAIL") ?? "no-reply@minhaempresa.com";
            options.UseStartTls = bool.TryParse(Environment.GetEnvironmentVariable("SMTP_USE_STARTTLS"), out var useTls) && useTls;
        });

        // Adicione o contexto do banco de dados
        var connectionString = Environment.GetEnvironmentVariable("MAILSHARP_DB_CONNECTION") ?? "Host=localhost;Port=5432;Database=mailsharp_db;Username=postgres;Password=postgres";
        services.AddMailSharpDatabase(connectionString);

        // Adicione o Worker como um serviço hospedado
        services.AddHostedService<EmailQueueWorker>();
    });

var host = builder.Build();

// Crie um escopo para resolver serviços antes de iniciar a aplicação
using (var scope = host.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
}

// Agora que a infraestrutura está pronta, inicie o host de forma assíncrona
await host.RunAsync();
