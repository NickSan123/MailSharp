using Microsoft.AspNetCore.Components.Forms;

namespace MailSharp.Web.Services;

public class EmailService
{
    private readonly HttpClient _http;

    public EmailService(HttpClient http) => _http = http;

    public async Task UploadEmailsAsync(IBrowserFile file, int templateId)
    {
        using var content = new MultipartFormDataContent();
        using var stream = file.OpenReadStream(file.Size);
        content.Add(new StreamContent(stream), "file", file.Name);
        content.Add(new StringContent(templateId.ToString()), "templateId");

        var response = await _http.PostAsync("/emails/upload", content);
        response.EnsureSuccessStatusCode();
    }
}
