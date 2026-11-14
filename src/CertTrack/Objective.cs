namespace CertTrack;

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
}
