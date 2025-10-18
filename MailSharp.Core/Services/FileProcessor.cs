using CsvHelper;
using MiniExcelLibs;
using System.Globalization;
using System.Text.Json;

namespace MailSharp.Core.Services;

public class FileProcessor : IFileProcessor
{
    public IEnumerable<string> ReadEmailsFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Arquivo não encontrado", filePath);

        var ext = Path.GetExtension(filePath).ToLower();

        return ext switch
        {
            ".csv" => ProcessCsv(filePath),
            ".json" => ProcessJson(filePath),
            ".xlsx" or ".xls" => ProcessExcel(filePath),
            ".txt" => ProcessText(filePath),
            _ => throw new NotSupportedException($"Extensão {ext} não suportada")
        };
    }

    private static IEnumerable<string> ProcessCsv(string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var emails = new List<string>();

        while (csv.Read())
        {
            for (int i = 0; i < csv.HeaderRecord?.Length; i++)
            {
                var value = csv.GetField(i)?.Trim();
                if (!string.IsNullOrEmpty(value) && value.Contains("@"))
                {
                    emails.Add(value);
                }
            }
        }

        return emails.Distinct();
    }

    private static IEnumerable<string> ProcessJson(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var emails = JsonSerializer.Deserialize<List<string>>(json);
        return emails?.Where(e => !string.IsNullOrEmpty(e) && e.Contains("@")).Distinct()
               ?? Enumerable.Empty<string>();
    }

    private static IEnumerable<string> ProcessExcel(string filePath)
    {
        var rows = MiniExcel.Query(filePath);
        var emails = new List<string>();

        foreach (var row in rows)
        {
            if (row is IDictionary<string, object> dict)
            {
                foreach (var value in dict.Values)
                {
                    var email = value?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(email) && email.Contains("@"))
                    {
                        emails.Add(email);
                    }
                }
            }
        }

        return emails.Distinct();
    }

    private static IEnumerable<string> ProcessText(string filePath)
    {
        return File.ReadAllLines(filePath)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrEmpty(line) && line.Contains("@"))
            .Distinct();
    }
}