using DotNetEnv;
using MailSharp.ApiService.Endpoints;
using MailSharp.Core.Config;
using MailSharp.Core.Extensions;
using MailSharp.Core.Repository;
using MailSharp.Core.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Aspire defaults
builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddLogging();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHttpClient();
Env.Load();

// Serviços internos
builder.Services.AddSingleton<IFileProcessor, FileProcessor>();

// RabbitMQ: agora lê também o virtual host da variável de ambiente RABBITMQ_VHOST
builder.Services.AddRabbitMQ(options =>
{
    options.HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
    options.Port = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672");
    options.UserName = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? "guest";
    options.Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest";
    options.ClientProvidedName = Environment.GetEnvironmentVariable("CLIENT_PROVIDED_NAME") ?? "mailsharp-service";

    // novo: virtual host
    options.VirtualHost = Environment.GetEnvironmentVariable("RABBITMQ_VHOST") ?? "/";
 });

// SMTP
builder.Services.AddMailService(options =>
{
    options.Host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "";
    options.Port = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var port) ? port : 587;
    options.Username = Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? "";
    options.Password = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? "";
    options.SenderName = Environment.GetEnvironmentVariable("SMTP_SENDER_NAME") ?? "Online Telecom";
    options.SenderEmail = Environment.GetEnvironmentVariable("SMTP_SENDER_EMAIL") ?? "no-reply@minhaempresa.com";
    options.UseStartTls = bool.TryParse(Environment.GetEnvironmentVariable("SMTP_USE_STARTTLS"), out var useTls) && useTls;
});

// Banco de dados
var connectionString = Environment.GetEnvironmentVariable("MAILSHARP_DB_CONNECTION");
builder.Services.AddMailSharpDatabase(connectionString);

#region Autenticação JWT via Cookie (SSO)

// Melhoria: Carregar a chave pública de forma mais segura e centralizada
var publicKeyPem = Environment.GetEnvironmentVariable("PUBLIC_RSA");
if (string.IsNullOrWhiteSpace(publicKeyPem))
{
    throw new InvalidOperationException("A variável de ambiente PUBLIC_RSA não foi encontrada.");
}

using var rsa = RSA.Create();
rsa.ImportFromPem(publicKeyPem);
var rsaKey = new RsaSecurityKey(rsa);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // IMPORTANTE: Em produção, use RequireHttpsMetadata = true
    //options.RequireHttpsMetadata = Environment.IsProduction();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = "SSO-auth",
        ValidateAudience = false, // Defina uma audiência se seu token tiver
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1),
        IssuerSigningKey = rsaKey,
        ValidateIssuerSigningKey = true
    };

    options.Events = new JwtBearerEvents
    {
        // Este é o lugar correto para dizer ao handler onde encontrar o token
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.ContainsKey("access_token"))
            {
                context.Token = context.Request.Cookies["access_token"];
            }
            return Task.CompletedTask;
        },
        OnChallenge = async context =>
        {
            // Interceptar a resposta 401 para um formato JSON personalizado
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized",
                message = "Token expirado ou inválido."
            });
        }
    };
});

builder.Services.AddAuthorization();

#endregion

var app = builder.Build();

// Pipeline de Middleware - A ORDEM É CRÍTICA
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// 1. Adicione o middleware de Autenticação
app.UseAuthentication();

// 2. Adicione o middleware de Autorização
app.UseAuthorization();

// Swagger / OpenAPI
app.MapOpenApi();
app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "v1"));

// Endpoints modulares
app.MapTemplateEndpoints();
app.MapUploadEmailRoutes(); // Minimal API endpoint

// Teste rápido
app.MapGet("/", () => "Ok").AllowAnonymous();

// Blazor / Razor Components
app.MapDefaultEndpoints();

app.Run();