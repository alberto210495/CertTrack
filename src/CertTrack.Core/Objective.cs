namespace CertTrack.Core;

public enum Status
{
    NotStarted,
    Studying,
    Done
}

public class Objective
{
    public int Id { get; set; }
    public required string Cert { get; set; }
    public int Domain { get; set; }
    public required string Title { get; set; }
    public Status Status { get; set; } = Status.NotStarted;
    public DateTime? LastReviewed { get; set; }

    // Changes the status. Moving to Studying or Done counts as a review.
    public void MoveTo(Status status, DateTime now)
    {
        Status = status;
        if (status != Status.NotStarted)
            LastReviewed = now;
    }
}
