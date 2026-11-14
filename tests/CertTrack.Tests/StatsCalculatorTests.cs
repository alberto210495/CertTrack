namespace CertTrack.Tests;

public class StatsCalculatorTests
{
    private static Objective Make(string cert, int domain, Status status)
    {
        return new Objective { Cert = cert, Domain = domain, Title = "Title", Status = status };
    }

    [Fact]
    public void ByDomain_ReturnsEmptyList_WhenThereAreNoObjectives()
    {
        var stats = StatsCalculator.ByDomain([]);

        Assert.Empty(stats);
    }

    [Fact]
    public void ByDomain_CountsDoneAndTotalPerDomain()
    {
        var objectives = new[]
        {
            Make("A+", 1, Status.Done),
            Make("A+", 1, Status.NotStarted),
            Make("A+", 2, Status.Done)
        };

        var stats = StatsCalculator.ByDomain(objectives);

        Assert.Equal(2, stats.Count);
        Assert.Equal(new DomainStats("A+", 1, Done: 1, Total: 2), stats[0]);
        Assert.Equal(new DomainStats("A+", 2, Done: 1, Total: 1), stats[1]);
    }

    [Fact]
    public void ByDomain_DoesNotCountStudyingAsDone()
    {
        var objectives = new[]
        {
            Make("A+", 1, Status.Studying),
            Make("A+", 1, Status.Studying)
        };

        var stats = StatsCalculator.ByDomain(objectives);

        Assert.Equal(new DomainStats("A+", 1, Done: 0, Total: 2), Assert.Single(stats));
    }

    [Fact]
    public void ByDomain_KeepsTheSameDomainNumberSeparatePerCert()
    {
        var objectives = new[]
        {
            Make("A+", 1, Status.Done),
            Make("Network+", 1, Status.NotStarted)
        };

        var stats = StatsCalculator.ByDomain(objectives);

        Assert.Equal(2, stats.Count);
        Assert.Equal(new DomainStats("A+", 1, Done: 1, Total: 1), stats[0]);
        Assert.Equal(new DomainStats("Network+", 1, Done: 0, Total: 1), stats[1]);
    }

    [Fact]
    public void ByDomain_SortsByCertThenDomain()
    {
        var objectives = new[]
        {
            Make("Network+", 2, Status.NotStarted),
            Make("A+", 3, Status.NotStarted),
            Make("Network+", 1, Status.NotStarted),
            Make("A+", 1, Status.NotStarted)
        };

        var stats = StatsCalculator.ByDomain(objectives);

        Assert.Equal(
            [("A+", 1), ("A+", 3), ("Network+", 1), ("Network+", 2)],
            stats.Select(s => (s.Cert, s.Domain)));
    }

    [Theory]
    [InlineData(0, 4, 0)]
    [InlineData(1, 4, 25)]
    [InlineData(1, 3, 33.33)]
    [InlineData(4, 4, 100)]
    [InlineData(0, 0, 0)]
    public void Percent_IsDoneOutOfTotal(int done, int total, double expected)
    {
        var stats = new DomainStats("A+", 1, done, total);

        Assert.Equal(expected, stats.Percent, precision: 2);
    }
}
