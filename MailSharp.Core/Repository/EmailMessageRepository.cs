using MailSharp.Core.Database;
using MailSharp.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace MailSharp.Core.Repository;


public class EmailMessageRepository(MailSharpDbContext db) : IEmailMessageRepository
{
    private readonly MailSharpDbContext _db = db;

    public async Task AddAsync(EmailMessageEntity emailMessage)
    {
        _db.EmailMessages.Add(emailMessage);
        await _db.SaveChangesAsync();
    }
    public async Task<IEnumerable<EmailMessageEntity>> GetAsync()
    {
        return await _db.EmailMessages.ToListAsync();
    }
}