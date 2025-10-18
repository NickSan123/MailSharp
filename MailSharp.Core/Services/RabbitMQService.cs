using MailSharp.Core.Models;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MailSharp.Core.Services;

public class RabbitMQService(IOptions<RabbitMQOptions> options, ILogger<RabbitMQService> logger) : IRabbitMQService, IAsyncDisposable
{
    private readonly RabbitMQOptions _options = options.Value;
    private readonly ILogger<RabbitMQService> _logger = logger;
    private IConnection? _connection;
    private bool _disposed;

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_connection != null && _connection.IsOpen)
            return _connection;

        await InitializeConnectionAsync(cancellationToken);
        return _connection!;
    }

    public async Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        return await connection.CreateChannelAsync(null, cancellationToken);
    }

    private async Task InitializeConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                ClientProvidedName = _options.ClientProvidedName
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _logger.LogInformation("Conexão RabbitMQ estabelecida com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao conectar com RabbitMQ");
            throw;
        }
    }

    public async Task CreateQueueWithRetryAsync<T>(
        string queueName,
        Func<T, Task> messageHandler,
        int maxRetries = 3,
        CancellationToken cancellationToken = default, bool durable = true)
    {
        IChannel channel = await CreateChannelAsync(cancellationToken);

        // Declarar fila principal
        var args = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = "",
            ["x-dead-letter-routing-key"] = $"{queueName}_dead"
        };
        await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: args);

        // Declarar DLQ
        await channel.QueueDeclareAsync(queue: $"{queueName}_dead", durable: true, exclusive: false, autoDelete: false, arguments: null);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<T>(json);

                if (message != null)
                    await messageHandler(message);

                // Ack se processado com sucesso
                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao processar mensagem da fila {queueName}");

                int retryCount = 0;
                if (ea.BasicProperties.Headers != null && ea.BasicProperties.Headers.TryGetValue("x-retries", out var obj))
                {
                    retryCount = Convert.ToInt32(obj);
                }

                retryCount++;

                if (retryCount >= maxRetries)
                {
                    // Enviar para DLQ
                    await channel.BasicRejectAsync(ea.DeliveryTag, false);
                    _logger.LogWarning($"Mensagem enviada para {queueName}_dead após {retryCount} tentativas");
                }
                else
                {
                    var props = new BasicProperties
                    {
                        Persistent = durable,
                        MessageId = Guid.NewGuid().ToString(),
                        Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                    };
                    // Atualiza header e reenvia para fila
                    //var props = channel.CreateBasicProperties();
                    props.Persistent = true;
                    props.Headers = ea.BasicProperties.Headers ?? new Dictionary<string, object>();
                    props.Headers["x-retries"] = retryCount;

                    await channel.BasicPublishAsync(
                        exchange: "",
                        mandatory: false,
                        routingKey: queueName,
                        basicProperties: props,
                        body: ea.Body,
                        cancellationToken: cancellationToken
                    );


                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                    _logger.LogWarning($"Mensagem reencaminhada para fila {queueName} (tentativa {retryCount})");
                }
            }
        };

        await channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer);

        _logger.LogInformation($"Consumidor iniciado para a fila {queueName}");
    }

    public async Task EnsureQueueWithDeadLetterAsync(string queueName, CancellationToken cancellationToken = default, bool durable = true)
    {
        IChannel channel = await CreateChannelAsync(cancellationToken);

        // Fila principal com DLQ
        var args = new Dictionary<string, object?>()
        {
            ["x-dead-letter-exchange"] = "",
            ["x-dead-letter-routing-key"] = $"{queueName}_dead"
        };

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: durable,
            exclusive: false,
            autoDelete: false,
            arguments: args,
            cancellationToken: cancellationToken
        );

        // Fila morta
        await channel.QueueDeclareAsync(
            queue: $"{queueName}_dead",
            durable: durable,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken
        );
    }


    public async Task PublishAsync<T>(string queueName, T message, bool durable = true, CancellationToken cancellationToken = default)
    {
        IChannel? channel = null;

        try
        {
            channel = await CreateChannelAsync(cancellationToken);

            //// Declarar a fila
            //await channel.QueueDeclareAsync(
            //	queue: queueName,
            //	durable: durable,
            //	exclusive: false,
            //	autoDelete: false,
            //	arguments: null,
            //	cancellationToken: cancellationToken
            //);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = durable,
                MessageId = Guid.NewGuid().ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken
            );

            _logger.LogDebug($"Mensagem publicada na fila {queueName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao publicar mensagem na fila {queueName}");
            throw;
        }
        finally
        {
            if (channel != null)
            {
                await channel.CloseAsync(cancellationToken: cancellationToken);
            }
        }
    }

    public async Task PublishSimpleAsync<T>(string queueName, T message, CancellationToken cancellationToken = default)
    {
        IChannel? channel = null;

        try
        {
            channel = await CreateChannelAsync(cancellationToken);

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken
            );

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                body: body,
                cancellationToken: cancellationToken
            );

            _logger.LogDebug($"Mensagem publicada na fila {queueName} (simples)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao publicar mensagem na fila {queueName}");
            throw;
        }
        finally
        {
            if (channel != null)
            {
                await channel.CloseAsync(cancellationToken: cancellationToken);
            }
        }
    }

    public async Task ConsumeAsync<T>(string queueName, Func<T, Task> messageHandler, bool autoAck = false, CancellationToken cancellationToken = default)
    {
        var channel = await CreateChannelAsync(cancellationToken);

        try
        {
            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken
            );

            // ✅ CORREÇÃO: Usar AsyncEventingBasicConsumer que já é async por padrão
            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var message = JsonSerializer.Deserialize<T>(json);

                    if (message != null)
                    {
                        await messageHandler(message);
                    }

                    if (!autoAck)
                    {
                        await channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Erro ao processar mensagem da fila {queueName}");
                    if (!autoAck)
                    {
                        await channel.BasicNackAsync(ea.DeliveryTag, false, true, cancellationToken);
                    }
                }
            };

            await channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: autoAck,
                consumer: consumer,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation($"Consumidor iniciado para a fila {queueName}");

            // Manter o consumidor ativo
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao iniciar consumidor para a fila {queueName}");
            await channel.CloseAsync(cancellationToken: cancellationToken);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        try
        {
            if (_connection != null && _connection.IsOpen)
            {
                await _connection.CloseAsync();
                _connection.Dispose();
            }

            _disposed = true;
            _logger.LogInformation("Conexão RabbitMQ finalizada");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao finalizar conexão RabbitMQ");
        }
    }

}
