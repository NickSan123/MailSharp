using MailSharp.Core.Models;
using MailSharp.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace MailSharp.ApiService.Endpoints;

public static class EmailController
{
    public static void MapEmailEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/send", async (
                [FromBody] EmailMessage model,
                HttpResponse response,
                [FromServices] IEmailSender sender) =>
        {
            await sender.SendAsync(model);

            return Results.Ok(new { message = "Email sent successfully" });
        });
    }
}
