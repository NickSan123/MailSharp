using MailSharp.Web.Components;
using MailSharp.Web.Services;
using MailSharp.Web.Endpoints;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using MailSharp.Web.Models;
using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// 🔹 Autorização
builder.Services.AddAuthorization();

// 🔹 Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Registrar AuthService como typed HttpClient apontando para o SSO
builder.Services.AddHttpClient<AuthService>(client =>
{
    client.BaseAddress = new Uri("https://sso.online.dev.br/api/v1/");
});

// manter outros serviços (TemplateService, EmailService, etc.)
builder.Services.AddScoped<TemplateService>();
builder.Services.AddScoped<EmailService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddOutputCache();

// 🔹 Estado de autenticação Blazor
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

// carregar PEM da configuração (variável de ambiente ou appsettings)
var publicKeyPem = builder.Configuration["SSO:PublicKeyPem"] ?? builder.Configuration["SSO:PublicKey"];
if (string.IsNullOrWhiteSpace(publicKeyPem))
{
    // Não lançar aqui para não quebrar ambientes onde SSO não é usado, mas logue/ajuste conforme necessidade.
    var loggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
    var logger = loggerFactory.CreateLogger("Startup");
    logger.LogWarning("SSO:PublicKeyPem não encontrado nas configurações.");
}

// IMPORTANTE: crie o RsaSecurityKey uma vez e registre como singleton para NÃO descartar o RSA.
// Se o RSA for criado dentro de um using, ele será descartado e a validação falhará (causa comum de 401).
RsaSecurityKey? rsaKey = null;
if (!string.IsNullOrWhiteSpace(publicKeyPem))
{
    var rsa = RSA.Create();
    rsa.ImportFromPem(publicKeyPem.ToCharArray());
    rsaKey = new RsaSecurityKey(rsa);
    builder.Services.AddSingleton<SecurityKey>(rsaKey);
}

builder.Services.AddAuthentication(options =>
{
    // Usamos JwtBearer como esquema principal (lerá o cookie "access_token").
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // ler token do cookie "access_token"
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            if (ctx.Request.Cookies.TryGetValue("access_token", out var token))
            {
                ctx.Token = token;
            }
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            // Deixe o comportamento padrão (opcional: customizar resposta 401)
            return Task.CompletedTask;
        }
    };

    if (rsaKey != null)
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false, // ajuste para true e configure ValidIssuer se souber o issuer
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = rsaKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    }
})
// Mantemos cookie auth registrado (opcional) para compatibilidade com redirects/logout do framework.
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/acesso-negado";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

var app = builder.Build();

// Registrar endpoints do proxy de autenticação (grava cookies HttpOnly)
app.MapAuthProxyEndpoints();

// Endpoint opcional para setar tokens (se ainda quiser)
// app.MapPost("/auth/set-tokens", async (HttpContext http, LoginResponse? tokens) => { ... });


// 🔹 Pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();
app.UseOutputCache();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
