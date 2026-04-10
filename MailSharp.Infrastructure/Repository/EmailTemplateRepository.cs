using MailSharp.Infrastructure.Database;
using MailSharp.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace MailSharp.Infrastructure.Repository;

public class EmailTemplateRepository(MailSharpDbContext context) : IEmailTemplateRepository
{
    public async Task<IEnumerable<EmailTemplate>> GetAllAsync() =>
        await context.EmailTemplates.AsNoTracking().ToListAsync();

    public async Task<EmailTemplate?> GetByIdAsync(int id) =>
        await context.EmailTemplates.FindAsync(id);

    public async Task AddAsync(EmailTemplate template)
    {
        context.EmailTemplates.Add(template);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(EmailTemplate template)
    {
        context.EmailTemplates.Update(template);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var template = await context.EmailTemplates.FindAsync(id);
        if (template != null)
        {
            context.EmailTemplates.Remove(template);
            await context.SaveChangesAsync();
        }
    }
}
