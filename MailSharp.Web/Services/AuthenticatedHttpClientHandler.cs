using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace MailSharp.Web.Services
{
    public class AuthenticatedHttpClientHandler : HttpClientHandler
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public AuthenticatedHttpClientHandler(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var user = _contextAccessor.HttpContext?.User;
            var token = user?.Claims.FirstOrDefault(c => c.Type == "access_token")?.Value;

            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return base.SendAsync(request, cancellationToken);
        }
    }
}
