using CertTrack.Core;

namespace CertTrack.Api.Models;

// An objective that belongs to one user. The CLI uses the plain Objective, which has no owner.
public class UserObjective : Objective
{
    public int UserId { get; set; }
}
