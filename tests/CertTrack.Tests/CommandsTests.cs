namespace CertTrack.Tests;

public class CommandsTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"certtrack-tests-{Guid.NewGuid()}.json");
    private readonly ObjectiveStore _store;

    public CommandsTests()
    {
        _store = new ObjectiveStore(_path);
    }

    public void Dispose()
    {
        File.Delete(_path);
    }

    [Fact]
    public void Add_SavesANewObjective()
    {
        var exitCode = Commands.Add(["add", "1.1 Mobile device hardware", "--cert", "A+", "--domain", "1"], _store);

        Assert.Equal(0, exitCode);
        var objective = Assert.Single(_store.Load());
        Assert.Equal(1, objective.Id);
        Assert.Equal("A+", objective.Cert);
        Assert.Equal(1, objective.Domain);
        Assert.Equal("1.1 Mobile device hardware", objective.Title);
        Assert.Equal(Status.NotStarted, objective.Status);
        Assert.Null(objective.LastReviewed);
    }

    [Fact]
    public void Add_GivesEachObjectiveTheNextId()
    {
        Commands.Add(["add", "First", "--cert", "A+", "--domain", "1"], _store);
        Commands.Add(["add", "Second", "--cert", "A+", "--domain", "1"], _store);
        Commands.Add(["add", "Third", "--cert", "A+", "--domain", "2"], _store);

        Assert.Equal([1, 2, 3], _store.Load().Select(o => o.Id));
    }

    [Fact]
    public void Add_ReusesTheSpellingOfAStoredCert()
    {
        Commands.Add(["add", "First", "--cert", "A+", "--domain", "1"], _store);

        Commands.Add(["add", "Second", "--cert", "a+", "--domain", "1"], _store);

        Assert.All(_store.Load(), o => Assert.Equal("A+", o.Cert));
    }

    [Theory]
    [InlineData("add")]
    [InlineData("add --cert A+ --domain 1")]
    [InlineData("add Title --domain 1")]
    [InlineData("add Title --cert --domain 1")]
    [InlineData("add Title --cert A+")]
    [InlineData("add Title --cert A+ --domain one")]
    [InlineData("add Title --cert A+ --domain 0")]
    public void Add_FailsAndSavesNothing_WhenInputIsBad(string commandLine)
    {
        var exitCode = Commands.Add(commandLine.Split(' '), _store);

        Assert.Equal(1, exitCode);
        Assert.Empty(_store.Load());
    }

    [Fact]
    public void Done_MarksTheObjectiveDoneAndSetsLastReviewed()
    {
        Commands.Add(["add", "First", "--cert", "A+", "--domain", "1"], _store);
        Commands.Add(["add", "Second", "--cert", "A+", "--domain", "1"], _store);

        var exitCode = Commands.Done(["done", "2"], _store);

        Assert.Equal(0, exitCode);
        var objectives = _store.Load();
        Assert.Equal(Status.NotStarted, objectives[0].Status);
        Assert.Null(objectives[0].LastReviewed);
        Assert.Equal(Status.Done, objectives[1].Status);
        Assert.NotNull(objectives[1].LastReviewed);
    }

    [Fact]
    public void Study_MarksTheObjectiveStudyingAndSetsLastReviewed()
    {
        Commands.Add(["add", "First", "--cert", "A+", "--domain", "1"], _store);

        var exitCode = Commands.Study(["study", "1"], _store);

        Assert.Equal(0, exitCode);
        var objective = Assert.Single(_store.Load());
        Assert.Equal(Status.Studying, objective.Status);
        Assert.NotNull(objective.LastReviewed);
    }

    [Theory]
    [InlineData("done")]
    [InlineData("done abc")]
    [InlineData("done 99")]
    public void Done_FailsAndChangesNothing_WhenIdIsMissingInvalidOrUnknown(string commandLine)
    {
        Commands.Add(["add", "First", "--cert", "A+", "--domain", "1"], _store);

        var exitCode = Commands.Done(commandLine.Split(' '), _store);

        Assert.Equal(1, exitCode);
        Assert.Equal(Status.NotStarted, Assert.Single(_store.Load()).Status);
    }

    [Theory]
    [InlineData("list --status finished")]
    [InlineData("list --status")]
    [InlineData("list --cert")]
    public void List_Fails_WhenAFilterIsInvalid(string commandLine)
    {
        var exitCode = Commands.List(commandLine.Split(' '), _store);

        Assert.Equal(1, exitCode);
    }

    [Fact]
    public void List_Succeeds_WithValidFilters()
    {
        Commands.Add(["add", "First", "--cert", "A+", "--domain", "1"], _store);

        var exitCode = Commands.List(["list", "--cert", "a+", "--status", "NotStarted"], _store);

        Assert.Equal(0, exitCode);
    }
}
