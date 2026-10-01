using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Inventory.Application.Users.DTOs;
using Xunit;

namespace Inventory.Api.Tests;

public sealed class AuthFlowTests : IClassFixture<InventoryApiFactory>
{
    private readonly HttpClient _client;

    public AuthFlowTests(InventoryApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_Login_And_Get_Profile_Should_Succeed()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var username = $"staff-{suffix}";
        var password = "StrongPassword!123";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = $"{suffix}@example.com",
            Username = username,
            Password = password
        });
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Username = username,
            Password = password
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var token = await loginResponse.Content.ReadFromJsonAsync<TokenResponseDto>();
        Assert.False(string.IsNullOrWhiteSpace(token?.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(token?.RefreshToken));

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        var profileResponse = await _client.GetAsync("/api/auth/profile");

        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
    }

    [Fact]
    public async Task Register_With_Duplicate_Email_Should_Return_Conflict()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var email = $"duplicate-{suffix}@example.com";

        var firstResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Username = $"first-{suffix}",
            Password = "StrongPassword!123"
        });
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var duplicateResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email.ToUpperInvariant(),
            Username = $"second-{suffix}",
            Password = "StrongPassword!123"
        });

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }
}
