using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace StaticWebAppsAuthentication
{
    public class StaticWebAppsAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IConfiguration config;
        private readonly HttpClient http;

        public StaticWebAppsAuthenticationStateProvider(IConfiguration config, IWebAssemblyHostEnvironment environment)
        {
            this.config = config;
            this.http = new HttpClient { BaseAddress = new Uri(environment.BaseAddress) };
        }

        public async override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var authDataUrl = config.GetValue<string>("StaticWebAppsAuthentication:AuthDataUrl", "/.auth/me");
                var data = await http.GetFromJsonAsync<AuthData>(authDataUrl);

                var principal = data.ClientPrincipal;
                var identity = new ClaimsIdentity(principal.IdentityProvider);
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, principal.UserId));
                identity.AddClaim(new Claim(ClaimTypes.Name, principal.UserDetails));
                identity.AddClaims(principal.UserRoles.Select(r => new Claim(ClaimTypes.Role, r)));
                return new AuthenticationState(new ClaimsPrincipal(identity));
            }
            catch
            {
                return new AuthenticationState(new ClaimsPrincipal());
            }
        }
    }

    public class ClientPrincipal
    {
        public string IdentityProvider { get; set; }
        public string UserId { get; set; }
        public string UserDetails { get; set; }
        public IEnumerable<string> UserRoles { get; set; }
    }

    public class AuthData
    {
        public ClientPrincipal ClientPrincipal { get; set; }
    }

    public static class StaticWebAppsAuthenticationExtensions
    {
        public static IServiceCollection AddStaticWebAppsAuthentication(this IServiceCollection services)
        {
            return services
                .AddAuthorizationCore()
                .AddScoped<AuthenticationStateProvider, StaticWebAppsAuthenticationStateProvider>();
        }
    }
}