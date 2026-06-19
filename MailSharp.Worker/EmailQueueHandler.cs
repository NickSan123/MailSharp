using easy_rabbitmq.Abstractions;
using easy_rabbitmq.Consumer;
using MailSharp.Core.Entities;
using MailSharp.Core.Models;
using MailSharp.Core.Services;
using MailSharp.Core.Utils;
using MailSharp.Infrastructure.Repository;

namespace MailSharp.Worker;

[RabbitMQConsumer(exchange: "mailsharp.emails", queue: "mailsharp.emails", routingKey: "emails.send")]
public class EmailQueueHandler(ILogger<EmailQueueHandler> logger,
    IServiceProvider serviceProvider) : IRabbitMQHandler<EmailMessage>
{
    public async Task HandleAsync(EmailMessage message)
    {
        using var scope = serviceProvider.CreateScope();

        var emailRepository = scope.ServiceProvider.GetRequiredService<IEmailMessageRepository>();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        var entity = new EmailMessageEntity
        {
            CreatedAt = DateTime.UtcNow,
            Key = string.Empty
        };

        try
        {
            if (message == null || string.IsNullOrWhiteSpace(message.To))
            {
                logger.LogWarning("Mensagem inválida recebida");
                return;
            }

            // Idempotência forte via hash
            var key = KeyEncryptHelpers.GenerateKey(message);

            entity.To = message.To;
            entity.Subject = message.Subject;
            entity.Body = message.Body;
            entity.IsHtml = message.IsHtml;
            entity.Cc = message.Cc;
            entity.Bcc = message.Bcc;
            entity.Key = key;

            var alreadySent = await emailRepository.GetByEmailAndKeyAsync(message.To, key);

            if (alreadySent != null)
            {
                logger.LogInformation("E-mail duplicado ignorado: {Email}", message.To);
                return;
            }

            // Envio
            await sender.SendAsync(message);

            entity.SentSuccessfully = true;

            logger.LogInformation("E-mail enviado com sucesso para {Email}", message.To);
        }
        catch (Exception ex)
        {
            entity.SentSuccessfully = false;
            entity.ErrorMessage = ex.Message;

            logger.LogError(ex, "Erro ao processar e-mail para: {Email}", entity.To);

        }
        finally
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(entity.Key))
                {
                    await emailRepository.AddAsync(entity);

                    logger.LogInformation(
                        "Registro salvo. Email: {Email}, Sucesso: {Sucesso}",
                        entity.To,
                        entity.SentSuccessfully
                    );
                }
                else
                {
                    logger.LogWarning("Entity sem chave - não persistida");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao persistir status do e-mail");
            }
        }
        await Task.CompletedTask;
    }
    
}
