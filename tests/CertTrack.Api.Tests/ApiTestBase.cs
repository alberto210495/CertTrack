using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CertTrack.Api.Contracts;

namespace CertTrack.Api.Tests;

// Each test gets a fresh API and database, so tests cannot affect each other.
public abstract class ApiTestBase : IDisposable
{
    protected const string Password = "correct-horse-battery";

    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected readonly ApiFactory Factory = new();

    public void Dispose()
    {
        Factory.Dispose();
    }

    // Registers a new user and returns a client that sends their token with every request.
    protected async Task<HttpClient> LoggedInClient(string email = "user@example.com")
    {
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email, password = Password });
        response.EnsureSuccessStatusCode();

        var auth = await Read<AuthResponse>(response);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return client;
    }

    protected static async Task<ObjectiveResponse> CreateObjective(
        HttpClient client, string title, string cert = "A+", int domain = 1, string status = "NotStarted")
    {
        var response = await client.PostAsJsonAsync("/api/objectives", new { title, cert, domain, status });
        response.EnsureSuccessStatusCode();
        return await Read<ObjectiveResponse>(response);
    }

    protected static async Task<T> Read<T>(HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(Json);
        return value ?? throw new InvalidOperationException("The response body was empty.");
    }
}
