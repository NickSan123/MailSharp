using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MailSharp.Infrastructure.Database;

public class MailSharpDbContextFactory : IDesignTimeDbContextFactory<MailSharpDbContext>
{
    public MailSharpDbContext CreateDbContext(string[] args)
    {
        // Use a connection string fixa ou de variável de ambiente
        var connectionString = Environment.GetEnvironmentVariable("MAILSHARP_DB_CONNECTION")
                               ?? "Host=localhost;Port=5432;Database=mailsharp_db;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<MailSharpDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new MailSharpDbContext(optionsBuilder.Options);
    }
}
