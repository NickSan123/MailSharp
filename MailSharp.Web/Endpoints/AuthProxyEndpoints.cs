using System.Net.Http.Json;
using MailSharp.Web.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MailSharp.Web.Endpoints;

public static class AuthProxyEndpoints
{
    public static void MapAuthProxyEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/proxy-login", async (HttpContext http,
                                                LoginRequest login,
                                                IHttpClientFactory httpFactory,
                                                ILoggerFactory loggerFactory,
                                                CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("AuthProxyEndpoints");

            // Validação básica de entrada
            if (string.IsNullOrWhiteSpace(login.Email) || string.IsNullOrWhiteSpace(login.Password))
            {
                return Results.BadRequest(new { message = "Email e senha são obrigatórios." });
            }

            try
            {
                var client = httpFactory.CreateClient();
                // Chamar SSO diretamente (full URL) para evitar depender do BaseAddress
                var ssoUrl = "https://sso.online.dev.br/api/v1/auth/login";

                var ssoResponse = await client.PostAsJsonAsync(ssoUrl, login, ct);

                if (!ssoResponse.IsSuccessStatusCode)
                {
                    logger.LogWarning("SSO retornou {StatusCode} para {Email}", ssoResponse.StatusCode, login.Email);
                    if (ssoResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        return Results.Unauthorized();
                    return Results.StatusCode(StatusCodes.Status502BadGateway);
                }

                var tokens = await ssoResponse.Content.ReadFromJsonAsync<LoginResponse?>(cancellationToken: ct);
                if (tokens is null)
                {
                    logger.LogError("SSO retornou payload inválido para {Email}", login.Email);
                    return Results.StatusCode(StatusCodes.Status502BadGateway);
                }

                // Configurar cookies HttpOnly seguros
                var accessCookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddMinutes(15)
                };

                var refreshCookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                };

                http.Response.Cookies.Append("access_token", tokens.AccessToken ?? string.Empty, accessCookieOptions);
                http.Response.Cookies.Append("refresh_token", tokens.RefreshToken ?? string.Empty, refreshCookieOptions);

                // Opcional: também retornar um corpo leve se necessário
                return Results.Ok(new { message = "Login efetuado" });
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
            catch (Exception ex)
            {
                //var logger = loggerFactory.CreateLogger("AuthProxyEndpoints");
                logger.LogError(ex, "Erro ao autenticar via SSO");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        })
        // Evitar antiforgery automático para este exemplo se a chamada vier do client Blazor,
        // mas idealmente proteja via antiforgery/csrf em produção.
        .AllowAnonymous();
    }
}

// Modelos (use os modelos existentes caso já existam)
// Se já existir MailSharp.Web.Models.LoginRequest/LoginResponse remova estas duplicatas.
public record LoginRequest(string Email, string Password);
public record LoginResponse(string AccessToken, string RefreshToken);