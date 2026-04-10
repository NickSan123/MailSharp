using MailSharp.Core.Entities;

namespace MailSharp.Infrastructure.Repository;

public interface IEmailMessageRepository
{
    Task AddAsync(EmailMessageEntity emailMessage);
    Task<EmailMessageEntity?> GetByEmailAndKeyAsync(string email, string key);
}


