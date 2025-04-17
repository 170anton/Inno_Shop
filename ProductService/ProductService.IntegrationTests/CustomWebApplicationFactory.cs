using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProductService.API;
using ProductService.Infrastructure.Data;

namespace ProductService.IntegrationTests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("IntegrationTests");

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<ProductDbContext>>();
                services.RemoveAll<ProductDbContext>();

                services.AddDbContext<ProductDbContext>(opts =>
                    opts.UseInMemoryDatabase("InMemoryTestDb"));

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme    = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    "Test", opts => { });

                
                services.AddAuthorization(options =>
                    options.AddPolicy("Default", p => p.RequireAuthenticatedUser())
                );

            });
        }
    }

    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock
        ) : base(options, logger, encoder, clock) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var header = Context.Request.Headers["Test-UserId"].FirstOrDefault();
            var userId = Guid.TryParse(header, out var g) 
                        ? header 
                        : "00000000-0000-0000-0000-000000000001";

            var claims   = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal= new ClaimsPrincipal(identity);
            var ticket   = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
