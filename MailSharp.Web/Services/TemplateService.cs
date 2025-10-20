using MailSharp.Web.Models;
using System.Net.Http.Json;

namespace MailSharp.Web.Services
{
    public class TemplateService
    {
        private readonly HttpClient _http;

        public TemplateService(HttpClient http)
        {
            _http = http;
        }

        private const string BaseUrl = "https://localhost:7455/api/templates"; // Ajuste para sua API

        public async Task<List<EmailTemplateDto>> GetAllAsync()
        {
            return await _http.GetFromJsonAsync<List<EmailTemplateDto>>(BaseUrl) ?? new List<EmailTemplateDto>();
        }

        public async Task<EmailTemplateDto?> GetByIdAsync(int id)
        {
            return await _http.GetFromJsonAsync<EmailTemplateDto>($"{BaseUrl}/{id}");
        }

        public async Task CreateAsync(EmailTemplateDto template)
        {
            await _http.PostAsJsonAsync(BaseUrl, template);
        }

        public async Task UpdateAsync(EmailTemplateDto template)
        {
            await _http.PutAsJsonAsync($"{BaseUrl}/{template.Id}", template);
        }

        public async Task DeleteAsync(int id)
        {
            await _http.DeleteAsync($"{BaseUrl}/{id}");
        }
    }
}
