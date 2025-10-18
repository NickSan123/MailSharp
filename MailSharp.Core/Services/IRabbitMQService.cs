// IRabbitMQService.cs
using RabbitMQ.Client;

namespace MailSharp.Core.Services;

public interface IRabbitMQService : IAsyncDisposable
{
    Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default);
    Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default);
    Task PublishAsync<T>(string queueName, T message, bool durable = true, CancellationToken cancellationToken = default);
    Task PublishSimpleAsync<T>(string queueName, T message, CancellationToken cancellationToken = default);
    Task ConsumeAsync<T>(string queueName, Func<T, Task> messageHandler, bool autoAck = false, CancellationToken cancellationToken = default);
	Task CreateQueueWithRetryAsync<T>(
		string queueName,
		Func<T, Task> messageHandler,
		int maxRetries = 3,
		CancellationToken cancellationToken = default, bool durable = true);

	Task EnsureQueueWithDeadLetterAsync(string queueName, CancellationToken cancellationToken = default, bool durable = true);

}