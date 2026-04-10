using easy_rabbitmq.Abstractions;
using easy_rabbitmq.Configuration;
using easy_rabbitmq.Topology;
using MailSharp.Core.Entities;
using MailSharp.Core.Models;
using MailSharp.Core.Services;
using MailSharp.Core.Utils;
using MailSharp.Infrastructure.Repository;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MailSharp.Worker;

public class EmailQueueWorker(
    ILogger<EmailQueueWorker> logger,
    IServiceProvider serviceProvider
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EmailQueueWorker iniciado");

        var pool = serviceProvider.GetRequiredService<IRabbitMQChannelPool>();
        var channel = await pool.RentAsync(stoppingToken);

        try
        {
            var topology = new RabbitMQTopology
            {
                Exchange = "mailsharp.emails",
                ExchangeType = easy_rabbitmq.Enums.RabbitMQExchangeType.Direct,
                Durable = true,
                Queues =
                [
                    new()
                    {
                        Queue = "mailsharp.emails",
                        RoutingKey = "emails.send",
                        Durable = true
                    }
                ],
                Retry = new RabbitMQRetryOptions
                {
                    Enabled = true,
                    Delays = [10, 30, 60], // segundos
                    RetrySuffix = ".retry",
                    DeadSuffix = ".dead"
                }
            };

            // Garante que tudo existe
            await RabbitMQTopologyBuilder.DeclareAsync(channel, topology, stoppingToken);

            // Limita concorrência
            await channel.BasicQosAsync(0, 10, false, stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                using var scope = serviceProvider.CreateScope();

                var emailRepository = scope.ServiceProvider.GetRequiredService<IEmailMessageRepository>();
                var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                var entity = new EmailMessageEntity
                {
                    CreatedAt = DateTime.UtcNow,
                    Key = string.Empty
                };

                var acked = false;

                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var message = JsonSerializer.Deserialize<EmailMessage>(json);

                    if (message == null || string.IsNullOrWhiteSpace(message.To))
                    {
                        logger.LogWarning("Mensagem inválida recebida");
                        await channel.BasicAckAsync(ea.DeliveryTag, false);
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
                        await channel.BasicAckAsync(ea.DeliveryTag, false);
                        acked = true;
                        return;
                    }

                    // Envio
                    await sender.SendAsync(message);

                    entity.SentSuccessfully = true;

                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                    acked = true;

                    logger.LogInformation("E-mail enviado com sucesso para {Email}", message.To);
                }
                catch (Exception ex)
                {
                    entity.SentSuccessfully = false;
                    entity.ErrorMessage = ex.Message;

                    logger.LogError(ex, "Erro ao processar e-mail para: {Email}", entity.To);

                    if (!acked)
                    {
                        // envia para retry / DLQ
                        await channel.BasicNackAsync(ea.DeliveryTag, false, false);
                    }
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
            };

            // Registra consumer UMA vez
            await channel.BasicConsumeAsync(
                queue: topology.Queues[0].Queue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken
            );

            logger.LogInformation("Consumer registrado com sucesso");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        finally
        {
            pool.Return(channel);
            logger.LogInformation("Channel devolvido ao pool");
        }
    }
}