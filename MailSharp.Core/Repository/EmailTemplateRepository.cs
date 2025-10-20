using MailSharp.Core.Database;
using MailSharp.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace MailSharp.Core.Repository;

public class EmailTemplateRepository : IEmailTemplateRepository
{
    private readonly MailSharpDbContext _context;

    public EmailTemplateRepository(MailSharpDbContext context) => _context = context;

    public async Task<IEnumerable<EmailTemplate>> GetAllAsync() =>
        await _context.EmailTemplates.AsNoTracking().ToListAsync();

    public async Task<EmailTemplate?> GetByIdAsync(int id) =>
        await _context.EmailTemplates.FindAsync(id);

    public async Task AddAsync(EmailTemplate template)
    {
        _context.EmailTemplates.Add(template);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(EmailTemplate template)
    {
        _context.EmailTemplates.Update(template);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var template = await _context.EmailTemplates.FindAsync(id);
        if (template != null)
        {
            _context.EmailTemplates.Remove(template);
            await _context.SaveChangesAsync();
        }
    }
}
