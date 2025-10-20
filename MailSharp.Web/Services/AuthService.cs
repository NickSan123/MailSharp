using System.Net.Http.Json;
using MailSharp.Web.Models;

namespace MailSharp.Web.Services
{
    public class AuthService
    {
        private readonly HttpClient _http;

        public AuthService(HttpClient http)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
        }

        public async Task<LoginResponse?> LoginAsync(LoginRequest model)
        {
            // ⚠️ O endpoint deve ser relativo, pois o BaseAddress está configurado no Program.cs
            var response = await _http.PostAsJsonAsync("auth/login", model);

            if (!response.IsSuccessStatusCode)
                throw new Exception("Usuário ou senha inválidos.");

            return await response.Content.ReadFromJsonAsync<LoginResponse>();
        }
    }
}
