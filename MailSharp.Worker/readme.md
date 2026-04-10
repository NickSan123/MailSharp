# Email Queue Consumer (.NET 10)

Este guia mostra como configurar um consumer usando `easy_rabbitmq` em um Worker Service (.NET 10).

---

## Pré-requisitos

* .NET 10
* RabbitMQ rodando (local ou remoto)
* Pacotes necessários:

  * `easy_rabbitmq`
  * `RabbitMQ.Client`
```csharp
dotnet add package easy_rabbitmq

```
---


## Primeiro passo: Configurar o RabbitMQ no DI

No `Program.cs`:

```csharp
builder.Services.AddEasyRabbitMQ(options =>
{
    options.HostName = "localhost";
    options.ClientProvidedName = "mailsharp-consumer";
});
```

---

## Segundo passo: Declaração da topologia

```csharp
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
```

---

## Terceiro passo: Obter channel e declarar topologia

```csharp
var pool = serviceProvider.GetRequiredService<IRabbitMQChannelPool>();
var channel = await pool.RentAsync(stoppingToken);

await RabbitMQTopologyBuilder.DeclareAsync(channel, topology, stoppingToken);
```

---

## Quarto passo: Configurar QoS

```csharp
await channel.BasicQosAsync(0, 10, false, stoppingToken);
```

---

## Quinto passo: Criar o consumer

```csharp
var consumer = new AsyncEventingBasicConsumer(channel);
```

---

## Sexto passo: Processar mensagens

```csharp
consumer.ReceivedAsync += async (model, ea) =>
{
    using var scope = serviceProvider.CreateScope();

    var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

    try
    {
        var body = ea.Body.ToArray();
        var json = Encoding.UTF8.GetString(body);
        var message = JsonSerializer.Deserialize<EmailMessage>(json);

        if (message == null)
        {
            await channel.BasicAckAsync(ea.DeliveryTag, false);
            return;
        }

        await sender.SendAsync(message);

        await channel.BasicAckAsync(ea.DeliveryTag, false);
    }
    catch
    {
        await channel.BasicNackAsync(ea.DeliveryTag, false, false);
    }
};
```

---

## Sétimo passo: Registrar o consumer

```csharp
await channel.BasicConsumeAsync(
    queue: topology.Queues[0].Queue,
    autoAck: false,
    consumer: consumer,
    cancellationToken: stoppingToken
);
```

---

## Código completo

```csharp
using easy_rabbitmq.Abstractions;
using easy_rabbitmq.Configuration;
using easy_rabbitmq.Topology;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

public class EmailQueueWorker(
    ILogger<EmailQueueWorker> logger,
    IServiceProvider serviceProvider
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
                    Delays = [10, 30, 60],
                    RetrySuffix = ".retry",
                    DeadSuffix = ".dead"
                }
            };

            await RabbitMQTopologyBuilder.DeclareAsync(channel, topology, stoppingToken);

            await channel.BasicQosAsync(0, 10, false, stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                using var scope = serviceProvider.CreateScope();

                var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var message = JsonSerializer.Deserialize<EmailMessage>(json);

                    if (message == null)
                    {
                        await channel.BasicAckAsync(ea.DeliveryTag, false);
                        return;
                    }

                    await sender.SendAsync(message);

                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch
                {
                    await channel.BasicNackAsync(ea.DeliveryTag, false, false);
                }
            };

            await channel.BasicConsumeAsync(
                queue: topology.Queues[0].Queue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken
            );

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        finally
        {
            pool.Return(channel);
        }
    }
}
```
