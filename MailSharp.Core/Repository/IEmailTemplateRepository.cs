using MailSharp.Core.Entities;

namespace MailSharp.Core.Repository;

public interface IEmailTemplateRepository
{
    Task<IEnumerable<EmailTemplate>> GetAllAsync();
    Task<EmailTemplate?> GetByIdAsync(int id);
    Task AddAsync(EmailTemplate template);
    Task UpdateAsync(EmailTemplate template);
    Task DeleteAsync(int id);
}
