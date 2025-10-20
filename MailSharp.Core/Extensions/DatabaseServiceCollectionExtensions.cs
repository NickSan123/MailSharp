using MailSharp.Core.Database;
using MailSharp.Core.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MailSharp.Core.Extensions;

public static class DatabaseServiceCollectionExtensions
{
    public static IServiceCollection AddMailSharpDatabase(this IServiceCollection services, string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentException("Connection string for MailSharp database not provided.");

        services.AddDbContext<MailSharpDbContext>(options =>
        {
#if DEBUG
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();
#endif
            //if (connectionString.Contains("Filename="))
            //    options.UseSqlite(connectionString);
            //else
                options.UseNpgsql(connectionString);
        });
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
        services.AddScoped<IEmailMessageRepository, EmailMessageRepository>();


        return services;
    }
}