namespace MailSharp.Worker;

using MailSharp.Core.Models;
using MailSharp.Core.Services;
using Microsoft.Extensions.DependencyInjection;

public class EmailQueueWorker(ILogger<EmailQueueWorker> logger, IRabbitMQService rabbitMQService, IServiceProvider serviceProvider) : BackgroundService
{
    private readonly IRabbitMQService _rabbitMQService = rabbitMQService;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Email Consumer iniciado");

        await _rabbitMQService.ConsumeAsync<EmailMessage>(
            queueName: "mail_sharp",
            messageHandler: async message =>
            {
                using var scope = _serviceProvider.CreateScope();
               // await EnviarNotificacaoFcmAsync(message, scope.ServiceProvider, stoppingToken);
            },
            autoAck: false,
            cancellationToken: stoppingToken
        );


        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
