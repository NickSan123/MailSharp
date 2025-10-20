using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace MailSharp.ApiService.Middleware;

public class JwtAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _publicRsaKey;

    public JwtAuthMiddleware(RequestDelegate next, string publicRsaKey)
    {
        _next = next;
        _publicRsaKey = publicRsaKey;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        #region Ignorar rotas públicas
        if (context.Request.Path.StartsWithSegments("/swagger") ||
            context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path == "/")
        {
            await _next(context);
            return;
        }
        #endregion

        #region Captura do Token
        var token = context.Request.Cookies["access_token"] ??
                    context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

        if (string.IsNullOrEmpty(token))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized", message = "Token não encontrado." });
            return;
        }
        #endregion

        #region Validação
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(_publicRsaKey.ToCharArray());

            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "SSO-auth",
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                IssuerSigningKey = new RsaSecurityKey(rsa),
                ValidateIssuerSigningKey = true
            };

            var principal = tokenHandler.ValidateToken(token, validationParams, out _);
            context.User = principal;

            await _next(context);
        }
        catch (SecurityTokenExpiredException)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized", message = "Token expirado." });
        }
        catch (Exception)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized", message = "Token inválido." });
        }
        #endregion
    }
}