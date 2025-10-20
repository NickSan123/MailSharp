using MailSharp.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace MailSharp.Core.Database;

public class MailSharpDbContext(DbContextOptions<MailSharpDbContext> options) : DbContext(options)
{
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<EmailMessageEntity> EmailMessages => Set<EmailMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmailTemplate>(entity =>
        {
            entity.ToTable("email_templates");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Subject).HasMaxLength(255);
            entity.Property(e => e.Body).IsRequired();
        });

        modelBuilder.Entity<EmailMessageEntity>(entity =>
        {
            entity.ToTable("emails");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.To).HasMaxLength(255).IsRequired();
        });
    }
}