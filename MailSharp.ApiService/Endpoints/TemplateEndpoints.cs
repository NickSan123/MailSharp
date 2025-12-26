using MailSharp.Core.Database;
using MailSharp.Core.Entities;
using MailSharp.Core.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MailSharp.ApiService.Endpoints;

public static class TemplateEndpoints
{
    public static void MapTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/templates")
                       .WithTags("Templates");

        // 🔹 GET /api/templates
        group.MapGet("/", async ([FromServices] MailSharpDbContext db) =>
        {
            var templates = await db.EmailTemplates.ToListAsync();
            return Results.Ok(templates);
        }).AllowAnonymous();

        // 🔹 GET /api/templates/{id}
        group.MapGet("/{id:int}", async ([FromServices] MailSharpDbContext db, int id) =>
        {
            var template = await db.EmailTemplates.FindAsync(id);
            return template is not null ? Results.Ok(template) : Results.NotFound();
        }).AllowAnonymous();

        // 🔹 POST /api/templates
        group.MapPost("/", async ([FromServices] MailSharpDbContext db, [FromBody] EmailTemplate model) =>
        {
            model.CreatedAt = DateTime.UtcNow;
            db.EmailTemplates.Add(model);
            await db.SaveChangesAsync();
            return Results.Created($"/api/templates/{model.Id}", model);
        });

        // 🔹 PUT /api/templates/{id}
        group.MapPut("/{id:int}", async ([FromServices] MailSharpDbContext db, int id, [FromBody] EmailTemplate update) =>
        {
            var existing = await db.EmailTemplates.FindAsync(id);
            if (existing is null) return Results.NotFound();

            existing.Name = update.Name;
            existing.Subject = update.Subject;
            existing.Body = update.Body;
            existing.IsHtml = update.IsHtml;
            existing.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(existing);
        });

        // 🔹 DELETE /api/templates/{id}
        group.MapDelete("/{id:int}", async ([FromServices] MailSharpDbContext db, int id) =>
        {
            var existing = await db.EmailTemplates.FindAsync(id);
            if (existing is null) return Results.NotFound();

            db.EmailTemplates.Remove(existing);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}