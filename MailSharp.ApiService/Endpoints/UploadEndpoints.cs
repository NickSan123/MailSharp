using easy_rabbitmq.Abstractions;
using easy_rabbitmq.Configuration;
using MailSharp.ApiService.Dto;
using MailSharp.Core.Models;
using MailSharp.Core.Services;
using MailSharp.Infrastructure.Repository;
using Microsoft.AspNetCore.Mvc;

public static class UploadEndpoints
{
    public static void MapUploadEmailRoutes(this IEndpointRouteBuilder app)
    {
        app.MapPost("/emails/upload", async (
            [FromForm] UploadEmailsRequest request,
            IFileProcessor fileProcessor,
            IRabbitMQPublisher rabbitMQService,
            IEmailTemplateRepository repo,
            ILogger<Program> logger) =>
        {
            if (request.File == null || request.File.Length == 0)
                return Results.BadRequest("Arquivo inválido.");

            var template = await repo.GetByIdAsync(request.TemplateId);
            if (template == null)
                return Results.BadRequest("Template de e-mail não encontrado.");

            // 👉 leitura em memória (sem disco)
            using var stream = request.File.OpenReadStream();
            var emails = fileProcessor.ReadEmailsFromStream(stream, request.File.FileName)
    .ToList();

            if (emails.Count == 0)
                return Results.BadRequest("Nenhum e-mail encontrado no arquivo.");

            var topology = new RabbitMQTopology
            {
                Exchange = "mailsharp.emails",
                ExchangeType = easy_rabbitmq.Enums.RabbitMQExchangeType.Direct,
                Durable = true,
                Queues =
                [
                    new() { Queue = "mailsharp.emails", RoutingKey = "emails.send", Durable = true },
                ]
            };

            var queuedEmailsCount = 0;

            // paralelismo controlado
            var tasks = emails.Select(async email =>
            {
                var emailMessage = new EmailMessage
                {
                    To = email,
                    Subject = template.Subject,
                    Body = template.Body,
                    IsHtml = template.IsHtml
                };

                try
                {
                    await rabbitMQService.PublishAsync(
                        exchange: topology.Exchange,
                        message: emailMessage,
                        routingKey: "emails.send" // corrigido
                    );

                    Interlocked.Increment(ref queuedEmailsCount);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Falha ao enfileirar o e-mail para: {Email}", email);
                }
            });

            await Task.WhenAll(tasks);

            return Results.Ok(new
            {
                TotalEmails = emails.Count,
                QueuedEmails = queuedEmailsCount,
                Message = $"{queuedEmailsCount} e-mails foram enfileirados com sucesso para envio!"
            });
        })
        .Accepts<UploadEmailsRequest>("multipart/form-data")
        .WithName("UploadEmails")
        .WithTags("Emails")
        .DisableAntiforgery()
        .AllowAnonymous();
    }
}