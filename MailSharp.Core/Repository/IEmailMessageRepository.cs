using MailSharp.Core.Database;
using MailSharp.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MailSharp.Core.Repository;

public interface IEmailMessageRepository
{
    Task AddAsync(EmailMessageEntity emailMessage);
    Task<IEnumerable<EmailMessageEntity>> GetAsync();
}


