using MailSharp.Core.Entities;
using MailSharp.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace MailSharp.Infrastructure.Repository;


public class EmailMessageRepository(MailSharpDbContext db) : IEmailMessageRepository
{
    private readonly MailSharpDbContext _db = db;

    public async Task AddAsync(EmailMessageEntity emailMessage)
    {
        _db.EmailMessages.Add(emailMessage);
        await _db.SaveChangesAsync();
    }

    public async Task<EmailMessageEntity?> GetByEmailAndKeyAsync(string email, string key)
    {
        return await _db.EmailMessages.Where(x=> x.To == email && x.Key == key).FirstOrDefaultAsync();
    }
}