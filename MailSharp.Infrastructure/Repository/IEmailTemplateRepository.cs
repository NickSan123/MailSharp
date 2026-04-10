using MailSharp.Core.Entities;

namespace MailSharp.Infrastructure.Repository;

public interface IEmailTemplateRepository
{
    Task<IEnumerable<EmailTemplate>> GetAllAsync();
    Task<EmailTemplate?> GetByIdAsync(int id);
    Task AddAsync(EmailTemplate template);
    Task UpdateAsync(EmailTemplate template);
    Task DeleteAsync(int id);
}
