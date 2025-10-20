namespace MailSharp.Worker;

using MailSharp.Core.Entities;
using MailSharp.Core.Models;
using MailSharp.Core.Repository;
using MailSharp.Core.Services;
using Microsoft.Extensions.DependencyInjection;

public class EmailQueueWorker(ILogger<EmailQueueWorker> logger,
                        IRabbitMQService rabbitMQService,
                        IServiceProvider serviceProvider) : BackgroundService
{
    private readonly ILogger<EmailQueueWorker> _logger = logger;
    private readonly IRabbitMQService _rabbitMQService = rabbitMQService;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailQueueWorker iniciado");

        //await _rabbitMQService.EnsureQueueWithDeadLetterAsync("mail_sharp", stoppingToken); //

        await _rabbitMQService.ConsumeAsync<EmailMessage>(
            queueName: "mail_sharp",
            messageHandler: async message =>
            {
                using var scope = _serviceProvider.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                var repo = scope.ServiceProvider.GetRequiredService<IEmailMessageRepository>();

                var entity = new EmailMessageEntity
                {
                    To = message.To,
                    Subject = message.Subject,
                    Body = message.Body,
                    IsHtml = message.IsHtml,
                    Cc = message.Cc,
                    Bcc = message.Bcc,
                    CreatedAt = DateTime.UtcNow
                };

                try
                {
                    await sender.SendAsync(message);
                    entity.SentSuccessfully = true;
                    _logger.LogInformation("E-mail enviado para: {Email}", message.To);
                }
                catch (Exception ex)
                {
                    entity.SentSuccessfully = false;
                    entity.ErrorMessage = ex.Message;
                    _logger.LogError(ex, "Erro ao enviar e-mail para: {Email}", message.To);
                }

                await repo.AddAsync(entity);
            },
            autoAck: false,
            cancellationToken: stoppingToken
        );
    }
}

