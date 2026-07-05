using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FinanceSentry.Mcp.Abstractions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace FinanceSentry.Mcp.Tests;

public sealed class TransportAwareIdentityResolverTests
{
    [Fact]
    public void GetUserId_Prefers_HttpContext_User_When_Authenticated()
    {
        var startupUserId = Guid.NewGuid();
        var requestUserId = Guid.NewGuid();

        var startupResolver = CreateStartupResolver(startupUserId, "startup@example.com");
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = CreatePrincipal(requestUserId, "request@example.com")
            }
        };

        var resolver = new TransportAwareIdentityResolver(accessor, startupResolver);

        resolver.GetUserId().Should().Be(requestUserId);
        resolver.GetEmail().Should().Be("request@example.com");
        resolver.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public void GetUserId_Falls_Back_To_Startup_Token_When_No_HttpContext_User()
    {
        var startupUserId = Guid.NewGuid();
        var startupResolver = CreateStartupResolver(startupUserId, "startup@example.com");
        var resolver = new TransportAwareIdentityResolver(new HttpContextAccessor(), startupResolver);

        resolver.GetUserId().Should().Be(startupUserId);
        resolver.GetEmail().Should().Be("startup@example.com");
        resolver.IsConfigured.Should().BeTrue();
    }

    private static JwtIdentityResolver CreateStartupResolver(Guid userId, string email)
    {
        const string secret = "super-secret-key-for-mcp-tests-123456";
        var token = CreateJwt(secret, userId, email, audience: "mcp");
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = secret,
                ["Mcp:Token"] = token
            })
            .Build();

        return new JwtIdentityResolver(config, NullLogger<JwtIdentityResolver>.Instance);
    }

    private static ClaimsPrincipal CreatePrincipal(Guid userId, string email)
        => new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email)
        ], "test"));

    private static string CreateJwt(string secret, Guid userId, string email, string? audience = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email)
        };

        if (!string.IsNullOrWhiteSpace(audience))
            claims.Add(new Claim(JwtRegisteredClaimNames.Aud, audience));

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
