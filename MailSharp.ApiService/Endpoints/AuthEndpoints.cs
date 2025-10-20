using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace MailSharp.ApiService.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (
            [FromBody] LoginRequestDTO model,
            IHttpClientFactory httpClientFactory,
            HttpResponse response) =>
        {
            var client = httpClientFactory.CreateClient();
            var ssoUrl = "https://sso.online.dev.br/api/v1/auth/login";

            var ssoResponse = await client.PostAsJsonAsync(ssoUrl, model);

            if (!ssoResponse.IsSuccessStatusCode)
                return Results.Unauthorized();

            var tokens = await ssoResponse.Content.ReadFromJsonAsync<LoginResponseDTO>();

            // Define cookies seguros
            response.Cookies.Append("access_token", tokens!.AccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(15)
            });

            response.Cookies.Append("refresh_token", tokens.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return Results.Ok(new { message = "Login bem-sucedido" });
        });

        app.MapPost("/auth/logout", (HttpResponse response) =>
        {
            response.Cookies.Delete("access_token");
            response.Cookies.Delete("refresh_token");
            return Results.Ok(new { message = "Logout realizado" });
        });
    }
}

public record LoginRequestDTO(string Email, string Password);
public record LoginResponseDTO(string AccessToken, string RefreshToken);
