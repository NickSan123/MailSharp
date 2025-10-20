using MailSharp.ApiService.Dto;
using MailSharp.Core.Models; // Certifique-se de que EmailMessage está aqui
using MailSharp.Core.Repository;
using MailSharp.Core.Services; // Adicione o using para IRabbitMQService
using Microsoft.AspNetCore.Mvc;

namespace MailSharp.ApiService.Endpoints;

public static class UploadEndpoints
{
    public static void MapUploadEmailRoutes(this IEndpointRouteBuilder app)
    {
        app.MapPost("/emails/upload", async (
            [FromForm] UploadEmailsRequest request,
            IFileProcessor fileProcessor,
            // Remova o IEmailSender daqui
            // IEmailSender sender, 
            IRabbitMQService rabbitMQService, // Adicione o IRabbitMQService aqui
            IEmailTemplateRepository repo,
            ILogger<Program> logger) =>
        {
            var template = await repo.GetByIdAsync(request.TemplateId);
            if (template == null)
                return Results.BadRequest("Template de e-mail não encontrado.");

            var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + Path.GetExtension(request.File.FileName));
            await using (var stream = File.Create(tempPath))
                await request.File.CopyToAsync(stream);

            var emails = fileProcessor.ReadEmailsFromFile(tempPath).ToList();
            var queuedEmailsCount = 0;

            foreach (var email in emails)
            {
                // Crie a mensagem de e-mail
                var emailMessage = new EmailMessage
                {
                    To = email,
                    Subject = template.Subject,
                    Body = template.Body,
                    IsHtml = template.IsHtml
                };

                try
                {
                    // Publique a mensagem na fila em vez de enviar diretamente
                    await rabbitMQService.PublishAsync(
                        queueName: "mail_sharp", // O nome da fila deve ser o mesmo que o worker escuta
                        message: emailMessage
                    );
                    queuedEmailsCount++;
                }
                catch (Exception ex)
                {
                    // Se falhar ao adicionar na fila, logue o erro
                    logger.LogError(ex, "Falha ao enfileirar o e-mail para: {Email}", email);
                }
            }

            File.Delete(tempPath);

            // Altere a mensagem de retorno para refletir a nova ação
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
        .DisableAntiforgery().RequireAuthorization();
    }
}