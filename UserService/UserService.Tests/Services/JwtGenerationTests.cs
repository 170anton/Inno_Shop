using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Xunit;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using UserService.Domain.Entities;
using UserService.Infrastructure.Services;
using UserService.Application.Interfaces;
using UserService.Application.Services;

namespace UserService.Tests.Services
{
    public class TokenServiceTests
    {
        [Fact]
        public void GenerateJwtToken_ReturnsTokenWithExpectedClaims()
        {
            var inMemorySettings = new Dictionary<string, string>
            {
                {"Jwt:Key", "242g8rfr3es8tg9ag89asr9jas49a6t5h7as3h67a5grs6g"},
                {"Jwt:Issuer", "domain.com"},
                {"Jwt:Audience", "domain.com"},
                {"Jwt:ExpireMinutes", "30"}
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            ITokenService tokenService = new TokenService(configuration);
            
            var user = new User
            {
                Id = "123",
                Email = "test@example.com",
                UserName = "test@example.com"
            };

            var tokenString = tokenService.GenerateJwtToken(user);

            Assert.False(string.IsNullOrEmpty(tokenString));

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(tokenString);
            
            var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
            Assert.NotNull(subClaim);
            Assert.Equal("test@example.com", subClaim.Value);

            var idClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
            Assert.NotNull(idClaim);
            Assert.Equal("123", idClaim.Value);
        }
    }
}
