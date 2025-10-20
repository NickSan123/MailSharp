using MailSharp.Web.Components;
using MailSharp.Web.Services;
using MailSharp.Web.Endpoints;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using MailSharp.Web.Models;
using System;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// 🔹 Autenticação por Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/acesso-negado";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

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
