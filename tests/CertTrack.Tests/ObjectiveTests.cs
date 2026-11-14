using CertTrack.Core;

namespace CertTrack.Tests;

public class ObjectiveTests
{
    private static readonly DateTime Now = new(2026, 11, 13, 9, 30, 0);

    private static Objective Make()
    {
        return new Objective { Cert = "A+", Domain = 1, Title = "Title" };
    }

    [Theory]
    [InlineData(Status.Studying)]
    [InlineData(Status.Done)]
    public void MoveTo_SetsTheStatusAndLastReviewed(Status status)
    {
        var objective = Make();

        objective.MoveTo(status, Now);

        Assert.Equal(status, objective.Status);
        Assert.Equal(Now, objective.LastReviewed);
    }

    [Fact]
    public void MoveTo_NotStarted_KeepsTheLastReviewDate()
    {
        var objective = Make();
        objective.MoveTo(Status.Done, Now);

        objective.MoveTo(Status.NotStarted, Now.AddDays(1));

        Assert.Equal(Status.NotStarted, objective.Status);
        Assert.Equal(Now, objective.LastReviewed);
    }
}
