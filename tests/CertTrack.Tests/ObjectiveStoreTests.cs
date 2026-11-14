using System.Text.Json;

namespace CertTrack.Tests;

public class ObjectiveStoreTests : IDisposable
{
    // Each test gets its own temp file, so tests never touch a real objectives.json.
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"certtrack-tests-{Guid.NewGuid()}.json");

    public void Dispose()
    {
        File.Delete(_path);
    }

    [Fact]
    public void Load_ReturnsEmptyList_WhenFileDoesNotExist()
    {
        var store = new ObjectiveStore(_path);

        var objectives = store.Load();

        Assert.Empty(objectives);
    }

    [Fact]
    public void Load_ReturnsWhatWasSaved()
    {
        var store = new ObjectiveStore(_path);
        var reviewed = new DateTime(2026, 11, 13, 9, 30, 0);

        store.Save(
        [
            new Objective { Id = 1, Cert = "A+", Domain = 1, Title = "1.1 Mobile device hardware" },
            new Objective { Id = 2, Cert = "Network+", Domain = 3, Title = "3.1 Network operations", Status = Status.Done, LastReviewed = reviewed }
        ]);
        var objectives = store.Load();

        Assert.Equal(2, objectives.Count);

        Assert.Equal(1, objectives[0].Id);
        Assert.Equal("A+", objectives[0].Cert);
        Assert.Equal(1, objectives[0].Domain);
        Assert.Equal("1.1 Mobile device hardware", objectives[0].Title);
        Assert.Equal(Status.NotStarted, objectives[0].Status);
        Assert.Null(objectives[0].LastReviewed);

        Assert.Equal(2, objectives[1].Id);
        Assert.Equal("Network+", objectives[1].Cert);
        Assert.Equal(3, objectives[1].Domain);
        Assert.Equal(Status.Done, objectives[1].Status);
        Assert.Equal(reviewed, objectives[1].LastReviewed);
    }

    [Fact]
    public void Save_ReplacesThePreviousContent()
    {
        var store = new ObjectiveStore(_path);
        store.Save(
        [
            new Objective { Id = 1, Cert = "A+", Domain = 1, Title = "First" },
            new Objective { Id = 2, Cert = "A+", Domain = 1, Title = "Second" }
        ]);

        store.Save([new Objective { Id = 3, Cert = "A+", Domain = 2, Title = "Third" }]);

        var objective = Assert.Single(store.Load());
        Assert.Equal("Third", objective.Title);
    }

    [Fact]
    public void Save_WritesStatusAsText()
    {
        var store = new ObjectiveStore(_path);

        store.Save([new Objective { Id = 1, Cert = "A+", Domain = 1, Title = "Title", Status = Status.Studying }]);

        Assert.Contains("\"Status\": \"Studying\"", File.ReadAllText(_path));
    }

    [Fact]
    public void Save_WritesPlusSignWithoutEscaping()
    {
        var store = new ObjectiveStore(_path);

        store.Save([new Objective { Id = 1, Cert = "A+", Domain = 1, Title = "Title" }]);

        Assert.Contains("\"Cert\": \"A+\"", File.ReadAllText(_path));
    }

    [Fact]
    public void Load_Throws_WhenFileIsNotValidJson()
    {
        File.WriteAllText(_path, "{ not json");
        var store = new ObjectiveStore(_path);

        Assert.Throws<JsonException>(() => store.Load());
    }

    [Fact]
    public void Load_Throws_WhenAnObjectiveHasNoTitle()
    {
        File.WriteAllText(_path, """[{ "Id": 1, "Cert": "A+", "Domain": 1 }]""");
        var store = new ObjectiveStore(_path);

        Assert.Throws<JsonException>(() => store.Load());
    }
}
