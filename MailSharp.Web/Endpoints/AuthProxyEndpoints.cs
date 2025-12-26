using System.Net.Http.Json;
using MailSharp.Web.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace MailSharp.Web.Endpoints;

public static class AuthProxyEndpoints
{
    public static void MapAuthProxyEndpoints(this WebApplication app)
    {
        var ssoBase = app.Configuration["SSO:BaseUrl"] ?? "https://sso.online.dev.br/api/v1/";

        app.MapPost("/auth/proxy-login", async (HttpContext http,
                                                LoginRequest login,
                                                IHttpClientFactory httpFactory,
                                                ILoggerFactory loggerFactory,
                                                CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("AuthProxyEndpoints");

            if (string.IsNullOrWhiteSpace(login.Email) || string.IsNullOrWhiteSpace(login.Password))
                return Results.BadRequest(new { message = "Email e senha são obrigatórios." });

            try
            {
                var client = httpFactory.CreateClient();
                var ssoUrl = $"{ssoBase}auth/login";

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

                // Gravar cookies HttpOnly (access_token + refresh_token)
                AppendTokenCookies(http, tokens);

                return Results.Ok(new { message = "Login efetuado" });
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
            catch (Exception ex)
            {
                loggerFactory.CreateLogger("AuthProxyEndpoints").LogError(ex, "Erro ao autenticar via SSO");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        })
        .AllowAnonymous();

        // Endpoint de refresh: usa cookie refresh_token no request e atualiza os cookies
        app.MapPost("/auth/refresh", async (HttpContext http,
                                            IHttpClientFactory httpFactory,
                                            ILoggerFactory loggerFactory,
                                            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("AuthProxyEndpoints/Refresh");

            try
            {
                if (!http.Request.Cookies.TryGetValue("refresh_token", out var refreshToken) || string.IsNullOrEmpty(refreshToken))
                {
                    return Results.BadRequest(new { message = "Refresh token ausente." });
                }

                var client = httpFactory.CreateClient();
                var refreshUrl = $"{ssoBase}auth/refresh";

                var payload = new { refresh_token = refreshToken };
                var resp = await client.PostAsJsonAsync(refreshUrl, payload, ct);

                if (!resp.IsSuccessStatusCode)
                {
                    logger.LogWarning("SSO refresh retornou {Status} para refresh_token", resp.StatusCode);
                    return Results.StatusCode((int)resp.StatusCode);
                }

                var newTokens = await resp.Content.ReadFromJsonAsync<LoginResponse?>(cancellationToken: ct);
                if (newTokens is null)
                {
                    logger.LogError("SSO refresh retornou payload inválido");
                    return Results.StatusCode(StatusCodes.Status502BadGateway);
                }

                // Atualizar cookies
                AppendTokenCookies(http, newTokens);

                return Results.Ok(new { message = "Tokens atualizados" });
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
            catch (Exception ex)
            {
                loggerFactory.CreateLogger("AuthProxyEndpoints/Refresh").LogError(ex, "Erro no refresh de tokens");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        })
        .AllowAnonymous();
    }

    private static void AppendTokenCookies(HttpContext http, LoginResponse tokens)
    {
        var config = http.RequestServices.GetRequiredService<IConfiguration>();
        // Opcional: informe um domínio como ".online.dev.br" se quiser compartilhar entre subdomínios.
        var cookieDomain = config["SSO:CookieDomain"]; 

        var accessCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None, // permitir envio em cenários cross-site
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddMinutes(15)
        };

        var refreshCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        };

        if (!string.IsNullOrWhiteSpace(cookieDomain))
        {
            accessCookieOptions.Domain = cookieDomain;
            refreshCookieOptions.Domain = cookieDomain;
        }

        http.Response.Cookies.Append("access_token", tokens.AccessToken ?? string.Empty, accessCookieOptions);
        http.Response.Cookies.Append("refresh_token", tokens.RefreshToken ?? string.Empty, refreshCookieOptions);
    }
}

// Modelos (use os modelos existentes caso já existam)
// Se já existir MailSharp.Web.Models.LoginRequest/LoginResponse remova estas duplicatas.
public record LoginRequest(string Email, string Password);
public record LoginResponse(string AccessToken, string RefreshToken);