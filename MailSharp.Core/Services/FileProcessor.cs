using CsvHelper;
using MiniExcelLibs;
using System.Globalization;
using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MailSharp.Core.Services;

public class FileProcessor : IFileProcessor
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public IEnumerable<string> ReadEmailsFromStream(Stream stream, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLower();

        return ext switch
        {
            ".csv" => ProcessCsv(stream),
            ".json" => ProcessJson(stream),
            ".xlsx" or ".xls" => ProcessExcel(stream),
            ".txt" => ProcessText(stream),
            _ => throw new NotSupportedException($"Extensão {ext} não suportada")
        };
    }

    public IEnumerable<string> ProcessCsv(Stream stream)
    {
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var emails = new HashSet<string>();

        while (csv.Read())
        {
            for (int i = 0; csv.TryGetField(i, out string? value); i++)
            {
                var email = value?.Trim();
                if (IsValidEmail(email))
                    emails.Add(email!);
            }
        }

        return emails;
    }

    private static IEnumerable<string> ProcessJson(Stream stream)
    {
        var emails = JsonSerializer.Deserialize<List<string>>(stream);

        return emails?
            .Where(IsValidEmail)
            .Distinct() ?? Enumerable.Empty<string>();
    }

    private static IEnumerable<string> ProcessExcel(Stream stream)
    {
        var emails = new HashSet<string>();

        foreach (IDictionary<string, object> row in MiniExcel.Query(stream, useHeaderRow: true))
        {

            foreach (var email in row.Values.Where(x => !string.IsNullOrWhiteSpace(x.ToString()))
                    .SelectMany(x => x.ToString().Split([',', ';', '\n', '\r'],
                        StringSplitOptions.RemoveEmptyEntries))
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Where(x => MailAddress.TryCreate(x, out _))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList())
            {
                if (IsValidEmail(email.ToString()))
                    emails.Add(email.ToString());
            }
        }
            return emails;
    }

    private static IEnumerable<string> ProcessText(Stream stream)
    {
        using var reader = new StreamReader(stream);

        var emails = new HashSet<string>();

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine()?.Trim();
            if (IsValidEmail(line))
                emails.Add(line!);
        }

        return emails;
    }

    private static bool IsValidEmail(string? email)
    {
        return !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email);
    }
}