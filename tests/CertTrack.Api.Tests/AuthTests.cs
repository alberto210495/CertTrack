using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CertTrack.Api.Contracts;
using CertTrack.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CertTrack.Api.Tests;

public class AuthTests : ApiTestBase
{
    [Fact]
    public async Task Register_ReturnsATokenAndTheEmail()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new { email = "new@example.com", password = Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await Read<AuthResponse>(response);
        Assert.Equal("new@example.com", auth.Email);
        Assert.NotEmpty(auth.Token);
    }

    [Fact]
    public async Task Register_StoresAHashInsteadOfThePassword()
    {
        await LoggedInClient("new@example.com");

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleAsync();

        Assert.NotEqual(Password, user.PasswordHash);
        Assert.DoesNotContain(Password, user.PasswordHash);
    }

    [Fact]
    public async Task Register_SameEmailInDifferentCase_Returns409()
    {
        var client = await LoggedInClient("new@example.com");

        var response = await client.PostAsJsonAsync("/api/auth/register", new { email = " NEW@Example.com ", password = Password });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("", "correct-horse-battery")]
    [InlineData("not-an-email", "correct-horse-battery")]
    [InlineData("new@example.com", "")]
    [InlineData("new@example.com", "short")]
    public async Task Register_InvalidInput_Returns400(string email, string password)
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new { email, password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_CorrectPassword_ReturnsATokenThatWorks()
    {
        await LoggedInClient("new@example.com");
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "New@Example.com", password = Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await Read<AuthResponse>(response);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/objectives")).StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        await LoggedInClient("new@example.com");
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "new@example.com", password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "nobody@example.com", password = Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/objectives")]
    [InlineData("/api/objectives/1")]
    [InlineData("/api/stats")]
    public async Task ProtectedEndpoint_WithoutAToken_Returns401(string url)
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithAMadeUpToken_Returns401()
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.token");

        var response = await client.GetAsync("/api/objectives");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithATokenFromAnotherServer_Returns401()
    {
        // Another API instance has a different signing key, so its tokens must be rejected here.
        using var otherFactory = new ApiFactory();
        var otherClient = otherFactory.CreateClient();
        var registered = await otherClient.PostAsJsonAsync("/api/auth/register", new { email = "new@example.com", password = Password });
        var foreignToken = (await Read<AuthResponse>(registered)).Token;

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", foreignToken);

        var response = await client.GetAsync("/api/objectives");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
