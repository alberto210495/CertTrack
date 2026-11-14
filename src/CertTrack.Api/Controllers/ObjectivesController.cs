using CertTrack.Api.Auth;
using CertTrack.Api.Contracts;
using CertTrack.Api.Data;
using CertTrack.Api.Models;
using CertTrack.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CertTrack.Api.Controllers;

// Every query filters by the logged-in user, so nobody can see or change another user's objectives.
[ApiController]
[Authorize]
[Route("api/objectives")]
public class ObjectivesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<List<ObjectiveResponse>> GetAll(string? cert, Status? status)
    {
        var userId = User.GetUserId();
        var query = db.Objectives.Where(o => o.UserId == userId);

        if (!string.IsNullOrWhiteSpace(cert))
        {
            var wanted = cert.Trim().ToLower();
            query = query.Where(o => o.Cert.ToLower() == wanted);
        }

        if (status is not null)
            query = query.Where(o => o.Status == status);

        var objectives = await query
            .OrderBy(o => o.Cert).ThenBy(o => o.Domain).ThenBy(o => o.Id)
            .ToListAsync();

        return objectives.Select(ObjectiveResponse.From).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ObjectiveResponse>> GetById(int id)
    {
        var objective = await Find(id);
        if (objective is null)
            return NotFound();

        return ObjectiveResponse.From(objective);
    }

    [HttpPost]
    public async Task<ActionResult<ObjectiveResponse>> Create(ObjectiveRequest request)
    {
        var userId = User.GetUserId();

        var objective = new UserObjective
        {
            UserId = userId,
            Title = request.Title.Trim(),
            Cert = await ResolveCert(userId, request.Cert),
            Domain = request.Domain
        };
        objective.MoveTo(request.Status, DateTime.UtcNow);

        db.Objectives.Add(objective);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = objective.Id }, ObjectiveResponse.From(objective));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ObjectiveResponse>> Update(int id, ObjectiveRequest request)
    {
        var objective = await Find(id);
        if (objective is null)
            return NotFound();

        objective.Title = request.Title.Trim();
        objective.Cert = await ResolveCert(objective.UserId, request.Cert);
        objective.Domain = request.Domain;

        // Only a real status change counts as a review.
        if (objective.Status != request.Status)
            objective.MoveTo(request.Status, DateTime.UtcNow);

        await db.SaveChangesAsync();

        return ObjectiveResponse.From(objective);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var objective = await Find(id);
        if (objective is null)
            return NotFound();

        db.Objectives.Remove(objective);
        await db.SaveChangesAsync();

        return NoContent();
    }

    // Returns null for an objective that does not exist and for one that belongs to someone else,
    // so the response does not reveal which ids are in use.
    private Task<UserObjective?> Find(int id)
    {
        var userId = User.GetUserId();
        return db.Objectives.SingleOrDefaultAsync(o => o.Id == id && o.UserId == userId);
    }

    private async Task<string> ResolveCert(int userId, string cert)
    {
        var certsInUse = await db.Objectives
            .Where(o => o.UserId == userId)
            .Select(o => o.Cert)
            .Distinct()
            .ToListAsync();

        return CertName.Resolve(certsInUse, cert.Trim());
    }
}
