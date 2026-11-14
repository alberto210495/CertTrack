using System.Net;
using System.Net.Http.Json;
using CertTrack.Api.Contracts;
using CertTrack.Core;

namespace CertTrack.Api.Tests;

public class ObjectivesTests : ApiTestBase
{
    [Fact]
    public async Task Create_ReturnsTheNewObjective()
    {
        var client = await LoggedInClient();

        var response = await client.PostAsJsonAsync("/api/objectives", new { title = "  1.1 Mobile device hardware ", cert = "A+", domain = 1 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var objective = await Read<ObjectiveResponse>(response);
        Assert.Equal("1.1 Mobile device hardware", objective.Title);
        Assert.Equal("A+", objective.Cert);
        Assert.Equal(1, objective.Domain);
        Assert.Equal(Status.NotStarted, objective.Status);
        Assert.Null(objective.LastReviewed);
        Assert.EndsWith($"/api/objectives/{objective.Id}", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Create_WithStatusDone_CountsAsAReview()
    {
        var client = await LoggedInClient();

        var objective = await CreateObjective(client, "Install RAM", status: "Done");

        Assert.Equal(Status.Done, objective.Status);
        Assert.NotNull(objective.LastReviewed);
    }

    [Fact]
    public async Task Create_ReusesTheSpellingOfAnExistingCert()
    {
        var client = await LoggedInClient();
        await CreateObjective(client, "First", cert: "Network+");

        var second = await CreateObjective(client, "Second", cert: "network+");

        Assert.Equal("Network+", second.Cert);
    }

    [Theory]
    [InlineData("", "A+", 1)]
    [InlineData("   ", "A+", 1)]
    [InlineData("Title", "", 1)]
    [InlineData("Title", "A+", 0)]
    [InlineData("Title", "A+", 21)]
    [InlineData("Title", "A cert name that is longer than thirty characters", 1)]
    public async Task Create_InvalidInput_Returns400(string title, string cert, int domain)
    {
        var client = await LoggedInClient();

        var response = await client.PostAsJsonAsync("/api/objectives", new { title, cert, domain });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await GetAll(client));
    }

    [Fact]
    public async Task Create_TitleLongerThan200Characters_Returns400()
    {
        var client = await LoggedInClient();

        var response = await client.PostAsJsonAsync("/api/objectives", new { title = new string('x', 201), cert = "A+", domain = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("\"Finished\"")]
    [InlineData("2")]
    public async Task Create_UnknownStatus_Returns400(string statusJson)
    {
        var client = await LoggedInClient();
        var body = new StringContent(
            $$"""{"title":"Title","cert":"A+","domain":1,"status":{{statusJson}}}""",
            System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/objectives", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_SortsByCertThenDomain()
    {
        var client = await LoggedInClient();
        await CreateObjective(client, "Third", cert: "Network+", domain: 1);
        await CreateObjective(client, "Second", cert: "A+", domain: 2);
        await CreateObjective(client, "First", cert: "A+", domain: 1);

        var objectives = await GetAll(client);

        Assert.Equal(["First", "Second", "Third"], objectives.Select(o => o.Title));
    }

    [Fact]
    public async Task GetAll_FiltersByCertAndStatus()
    {
        var client = await LoggedInClient();
        await CreateObjective(client, "A+ done", cert: "A+", status: "Done");
        await CreateObjective(client, "A+ not started", cert: "A+");
        await CreateObjective(client, "Network+ done", cert: "Network+", status: "Done");

        var objectives = await GetAll(client, "?cert=a%2B&status=done");

        Assert.Equal("A+ done", Assert.Single(objectives).Title);
    }

    [Fact]
    public async Task GetAll_UnknownStatusFilter_Returns400()
    {
        var client = await LoggedInClient();

        var response = await client.GetAsync("/api/objectives?status=finished");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var client = await LoggedInClient();

        var response = await client.GetAsync("/api/objectives/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesTheObjective()
    {
        var client = await LoggedInClient();
        var created = await CreateObjective(client, "Old title", cert: "A+", domain: 1);

        var response = await client.PutAsJsonAsync(
            $"/api/objectives/{created.Id}", new { title = "New title", cert = "Network+", domain = 4, status = "Studying" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await client.GetFromJsonAsync<ObjectiveResponse>($"/api/objectives/{created.Id}", Json);
        Assert.NotNull(saved);
        Assert.Equal("New title", saved.Title);
        Assert.Equal("Network+", saved.Cert);
        Assert.Equal(4, saved.Domain);
        Assert.Equal(Status.Studying, saved.Status);
        Assert.NotNull(saved.LastReviewed);
    }

    [Fact]
    public async Task Update_WithTheSameStatus_KeepsLastReviewed()
    {
        var client = await LoggedInClient();
        var created = await CreateObjective(client, "Old title", status: "Done");

        var response = await client.PutAsJsonAsync(
            $"/api/objectives/{created.Id}", new { title = "New title", cert = "A+", domain = 1, status = "Done" });

        var updated = await Read<ObjectiveResponse>(response);
        Assert.Equal("New title", updated.Title);
        Assert.Equal(created.LastReviewed, updated.LastReviewed);
    }

    [Fact]
    public async Task Update_InvalidInput_Returns400AndChangesNothing()
    {
        var client = await LoggedInClient();
        var created = await CreateObjective(client, "Old title");

        var response = await client.PutAsJsonAsync(
            $"/api/objectives/{created.Id}", new { title = "", cert = "A+", domain = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Old title", Assert.Single(await GetAll(client)).Title);
    }

    [Fact]
    public async Task Update_UnknownId_Returns404()
    {
        var client = await LoggedInClient();

        var response = await client.PutAsJsonAsync("/api/objectives/999", new { title = "Title", cert = "A+", domain = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesTheObjective()
    {
        var client = await LoggedInClient();
        var keep = await CreateObjective(client, "Keep");
        var remove = await CreateObjective(client, "Remove");

        var response = await client.DeleteAsync($"/api/objectives/{remove.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(keep.Id, Assert.Single(await GetAll(client)).Id);
    }

    [Fact]
    public async Task Delete_UnknownId_Returns404()
    {
        var client = await LoggedInClient();

        var response = await client.DeleteAsync("/api/objectives/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AnotherUser_CannotSeeTheObjective()
    {
        var owner = await LoggedInClient("owner@example.com");
        var other = await LoggedInClient("other@example.com");
        var objective = await CreateObjective(owner, "Private");

        Assert.Empty(await GetAll(other));
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/objectives/{objective.Id}")).StatusCode);
    }

    [Fact]
    public async Task AnotherUser_CannotChangeOrDeleteTheObjective()
    {
        var owner = await LoggedInClient("owner@example.com");
        var other = await LoggedInClient("other@example.com");
        var objective = await CreateObjective(owner, "Private");

        var update = await other.PutAsJsonAsync(
            $"/api/objectives/{objective.Id}", new { title = "Changed", cert = "A+", domain = 1, status = "Done" });
        var delete = await other.DeleteAsync($"/api/objectives/{objective.Id}");

        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        var unchanged = Assert.Single(await GetAll(owner));
        Assert.Equal("Private", unchanged.Title);
        Assert.Equal(Status.NotStarted, unchanged.Status);
    }

    [Fact]
    public async Task Stats_CountsDoneAndTotalPerDomain()
    {
        var client = await LoggedInClient();
        await CreateObjective(client, "One", cert: "A+", domain: 1, status: "Done");
        await CreateObjective(client, "Two", cert: "A+", domain: 1);
        await CreateObjective(client, "Three", cert: "A+", domain: 2, status: "Studying");

        var stats = await client.GetFromJsonAsync<List<DomainStats>>("/api/stats", Json);

        Assert.Equal([new DomainStats("A+", 1, 1, 2), new DomainStats("A+", 2, 0, 1)], stats);
    }

    [Fact]
    public async Task Stats_IgnoresOtherUsers()
    {
        var owner = await LoggedInClient("owner@example.com");
        var other = await LoggedInClient("other@example.com");
        await CreateObjective(owner, "Private", status: "Done");

        var stats = await other.GetFromJsonAsync<List<DomainStats>>("/api/stats", Json);

        Assert.NotNull(stats);
        Assert.Empty(stats);
    }

    private static async Task<List<ObjectiveResponse>> GetAll(HttpClient client, string query = "")
    {
        var objectives = await client.GetFromJsonAsync<List<ObjectiveResponse>>($"/api/objectives{query}", Json);
        return objectives ?? [];
    }
}
