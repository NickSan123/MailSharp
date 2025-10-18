using MailSharp.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MailSharp.Core.Services;

public interface IFileProcessor
{
    /// <summary>
    /// Lê o arquivo e retorna uma lista de emails
    /// </summary>
    /// <param name="filePath">Caminho do arquivo</param>
    /// <returns>Lista de EmailMessage</returns>
    IEnumerable<string> ReadEmailsFromFile(string filePath);
}
