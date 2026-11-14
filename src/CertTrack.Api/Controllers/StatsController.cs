using CertTrack.Api.Auth;
using CertTrack.Api.Data;
using CertTrack.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CertTrack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/stats")]
public class StatsController(AppDbContext db) : ControllerBase
{
    // Done and total per cert and domain, using the same calculator as the CLI.
    [HttpGet]
    public async Task<List<DomainStats>> Get()
    {
        var userId = User.GetUserId();
        var objectives = await db.Objectives.Where(o => o.UserId == userId).ToListAsync();

        return StatsCalculator.ByDomain(objectives);
    }
}
