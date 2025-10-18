using MailSharp.Core.Services;
using MailSharp.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

builder.Services.AddSingleton<IFileProcessor, FileProcessor>();
//builder.Services.AddSingleton<EmailQueueProducer>();

// Add logging
builder.Services.AddLogging();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();


builder.Services.AddRabbitMQ(options =>
{
    options.HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
    options.Port = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672");
    options.UserName = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? "guest";
    options.Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest";
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Mostra detalhes do erro em desenvolvimento
}
else
{
    app.UseExceptionHandler();
}

app.MapOpenApi();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "v1");
});

app.MapPost("/upload-emails", async (IFormFile file, IFileProcessor fileProcessor, ILogger<Program> logger) =>
{
    logger.LogInformation("Upload iniciado. Nome do arquivo: {FileName}", file?.FileName);

    if (file == null || file.Length == 0)
        return Results.BadRequest("Nenhum arquivo enviado.");

    // Validação do tipo de arquivo
    var allowedExtensions = new[] { ".xlsx", ".xls", ".csv", ".txt", ".json" };
    var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

    if (!allowedExtensions.Contains(fileExtension))
    {
        return Results.BadRequest($"Tipo de arquivo não suportado. Use: {string.Join(", ", allowedExtensions)}");
    }

    // CORREÇÃO: Salva com a extensão original em vez de .tmp
    var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + fileExtension);
    logger.LogInformation("Arquivo temporário criado: {TempPath}", tempPath);

    try
    {
        await using (var stream = File.Create(tempPath))
        {
            await file.CopyToAsync(stream);
        }

        logger.LogInformation("Arquivo salvo temporariamente. Tamanho: {Length} bytes", file.Length);

        // Processa o arquivo
        var emails = fileProcessor.ReadEmailsFromFile(tempPath);
        var emailList = emails.ToList();

        logger.LogInformation("E-mails processados: {Count} e-mails", emailList.Count);

        if (!emailList.Any())
        {
            return Results.BadRequest("Nenhum e-mail válido encontrado no arquivo.");
        }

        return Results.Ok(new
        {
            TotalEmails = emailList.Count,
            Emails = emailList.Take(10),
            Message = "E-mails processados com sucesso!"
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro ao processar arquivo: {ErrorMessage}", ex.Message);
        return Results.BadRequest(new { error = $"Erro ao processar arquivo: {ex.Message}" });
    }
    finally
    {
        try
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
                logger.LogInformation("Arquivo temporário removido: {TempPath}", tempPath);
            }
        }
        catch (Exception cleanupEx)
        {
            logger.LogWarning(cleanupEx, "Erro ao remover arquivo temporário");
        }
    }
})
.DisableAntiforgery()
.Accepts<IFormFile>("multipart/form-data")
.WithName("UploadEmails");

app.MapGet("/", () => "Ok");

app.MapDefaultEndpoints();

app.Run();