using System.ComponentModel.DataAnnotations;
using CertTrack.Api.Models;
using CertTrack.Core;

namespace CertTrack.Api.Contracts;

// The body of both "create" and "update".
public class ObjectiveRequest
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = "";

    [Required, MaxLength(30)]
    public string Cert { get; set; } = "";

    [Range(1, 20)]
    public int Domain { get; set; }

    public Status Status { get; set; } = Status.NotStarted;
}

// What the API sends back. It leaves out UserId on purpose.
public record ObjectiveResponse(int Id, string Cert, int Domain, string Title, Status Status, DateTime? LastReviewed)
{
    public static ObjectiveResponse From(UserObjective objective)
    {
        return new ObjectiveResponse(
            objective.Id, objective.Cert, objective.Domain, objective.Title, objective.Status, objective.LastReviewed);
    }
}
