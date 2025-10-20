using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace MailSharp.Web.Services
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public CustomAuthStateProvider(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var context = _contextAccessor.HttpContext;
            ClaimsPrincipal user = context?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
            return Task.FromResult(new AuthenticationState(user));
        }
    }
}
