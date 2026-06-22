using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MailSharp.Infrastructure.Database;

public class MailSharpDbContextFactory : IDesignTimeDbContextFactory<MailSharpDbContext>
{
    public MailSharpDbContext CreateDbContext(string[] args)
    {
        // Use a connection string fixa ou de variável de ambiente
        var connectionString = Environment.GetEnvironmentVariable("MAILSHARP_DB_CONNECTION")
                               ?? "Host=191.7.193.138;Port=5433;Database=MailSharp;Username=mobgo;Password=hJupDeWk4CxFT9rhyYtBGDos7bvC4tBxcnEkMQ34";

        var optionsBuilder = new DbContextOptionsBuilder<MailSharpDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new MailSharpDbContext(optionsBuilder.Options);
    }
}
