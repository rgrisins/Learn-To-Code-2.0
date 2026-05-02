using System.Security.Claims;
using LearnToCode.API.Contracts.Representations;
using LearnToCode.API.Services;
using LearnToCode.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnToCode.API.Controllers;

[ApiController]
[Authorize]
[Route("api/representations")]
public class RepresentationsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IAuthSessionService _authSessionService;

    public RepresentationsController(AppDbContext dbContext, IAuthSessionService authSessionService)
    {
        _dbContext = dbContext;
        _authSessionService = authSessionService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RepresentationResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var representations = await _dbContext.Representations
            .AsNoTracking()
            .OrderBy(representation => representation.Name)
            .ToListAsync(cancellationToken);

        return Ok(await BuildResponsesAsync(representations, userId.Value, cancellationToken));
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IEnumerable<RepresentationResponse>>> GetMine(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var representationIds = await _dbContext.RepresentationMemberships
            .AsNoTracking()
            .Where(membership => membership.UserId == userId.Value)
            .Select(membership => membership.RepresentationId)
            .ToListAsync(cancellationToken);

        var representations = await _dbContext.Representations
            .AsNoTracking()
            .Where(representation => representationIds.Contains(representation.Id))
            .OrderBy(representation => representation!.Name)
            .ToListAsync(cancellationToken);

        return Ok(await BuildResponsesAsync(representations, userId.Value, cancellationToken));
    }

    [HttpGet("{id:int}/members")]
    public async Task<ActionResult<IEnumerable<RepresentationMemberResponse>>> GetMembers(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var isMember = await _dbContext.RepresentationMemberships
            .AsNoTracking()
            .AnyAsync(membership => membership.RepresentationId == id && membership.UserId == userId.Value, cancellationToken);

        if (!isMember)
        {
            return Forbid();
        }

        // FullName ir computed → ielādējam pilnās entītes un mapojam C# atmiņā
        var memberships = await _dbContext.RepresentationMemberships
            .AsNoTracking()
            .Include(membership => membership.User)
            .Where(membership => membership.RepresentationId == id)
            .OrderBy(membership => membership.Role == RepresentationMemberRole.Owner ? 0 : 1)
            .ThenByDescending(membership => membership.User!.Rating)
            .ThenBy(membership => membership.User!.LastName)
            .ThenBy(membership => membership.User!.FirstName)
            .ToListAsync(cancellationToken);

        var members = memberships.Select(membership => new RepresentationMemberResponse
        {
            UserId = membership.UserId,
            Username = membership.User?.Username,
            FullName = membership.User?.FullName ?? string.Empty,
            Role = membership.Role.ToString(),
            Rating = membership.User?.Rating ?? 0,
            JoinedAtUtc = membership.JoinedAtUtc,
        }).ToList();

        return Ok(members);
    }

    [HttpPost]
    public async Task<ActionResult<RepresentationResponse>> Create([FromBody] RepresentationCreateRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var name = request.Name.Trim();
        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        var normalizedName = NormalizeName(name);

        if (name.Length is < 3 or > 160)
        {
            return BadRequest(new { message = "Pārstāvniecības nosaukumam jābūt 3-160 rakstzīmes garam." });
        }

        if (description?.Length > 800)
        {
            return BadRequest(new { message = "Pārstāvniecības apraksts ir par garu." });
        }

        var nameTaken = await _dbContext.Representations
            .AnyAsync(representation => representation.NormalizedName == normalizedName, cancellationToken);

        if (nameTaken)
        {
            return Conflict(new { message = "Pārstāvniecība ar šādu nosaukumu jau eksistē." });
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(existingUser => existingUser.Id == userId.Value, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var representation = new Representation
        {
            Name = name,
            NormalizedName = normalizedName,
            Description = description,
            CreatedByUserId = user.Id,
            CreatedAtUtc = DateTime.UtcNow,
        };

        _dbContext.Representations.Add(representation);
        _dbContext.RepresentationMemberships.Add(new RepresentationMembership
        {
            Representation = representation,
            UserId = user.Id,
            Role = RepresentationMemberRole.Owner,
            JoinedAtUtc = DateTime.UtcNow,
        });

        if (string.IsNullOrWhiteSpace(user.Representation))
        {
            user.Representation = name;
            user.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _authSessionService.RefreshUserSessionsAsync(user, cancellationToken);

        var responses = await BuildResponsesAsync([representation], user.Id, cancellationToken);
        return Ok(responses.Single());
    }

    [HttpPost("{id:int}/join")]
    public async Task<ActionResult<RepresentationResponse>> Join(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var representation = await _dbContext.Representations
            .FirstOrDefaultAsync(existingRepresentation => existingRepresentation.Id == id, cancellationToken);

        if (representation is null)
        {
            return NotFound(new { message = "Pārstāvniecība nav atrasta." });
        }

        var isMember = await _dbContext.RepresentationMemberships
            .AnyAsync(membership => membership.RepresentationId == id && membership.UserId == userId.Value, cancellationToken);

        var user = await _dbContext.Users.FirstOrDefaultAsync(existingUser => existingUser.Id == userId.Value, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        if (!isMember)
        {
            _dbContext.RepresentationMemberships.Add(new RepresentationMembership
            {
                RepresentationId = representation.Id,
                UserId = user.Id,
                Role = RepresentationMemberRole.Member,
                JoinedAtUtc = DateTime.UtcNow,
            });
        }

        if (string.IsNullOrWhiteSpace(user.Representation))
        {
            user.Representation = representation.Name;
            user.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _authSessionService.RefreshUserSessionsAsync(user, cancellationToken);

        var responses = await BuildResponsesAsync([representation], user.Id, cancellationToken);
        return Ok(responses.Single());
    }

    [HttpDelete("{id:int}/leave")]
    public async Task<IActionResult> Leave(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var membership = await _dbContext.RepresentationMemberships
            .Include(item => item.Representation)
            .FirstOrDefaultAsync(item => item.RepresentationId == id && item.UserId == userId.Value, cancellationToken);

        if (membership is null)
        {
            return NotFound(new { message = "Tu neesi šīs pārstāvniecības dalībnieks." });
        }

        var representationName = membership.Representation.Name;
        var user = await _dbContext.Users.FirstOrDefaultAsync(existingUser => existingUser.Id == userId.Value, cancellationToken);

        _dbContext.RepresentationMemberships.Remove(membership);

        var remainingMembers = await _dbContext.RepresentationMemberships
            .Where(item => item.RepresentationId == id && item.UserId != userId.Value)
            .OrderBy(item => item.JoinedAtUtc)
            .ToListAsync(cancellationToken);

        if (remainingMembers.Count == 0)
        {
            _dbContext.Representations.Remove(membership.Representation);
        }
        else if (membership.Role == RepresentationMemberRole.Owner && remainingMembers.All(item => item.Role != RepresentationMemberRole.Owner))
        {
            remainingMembers[0].Role = RepresentationMemberRole.Owner;
        }

        if (user is not null && string.Equals(user.Representation, representationName, StringComparison.OrdinalIgnoreCase))
        {
            var nextRepresentationName = await _dbContext.RepresentationMemberships
                .AsNoTracking()
                .Where(item => item.UserId == user.Id && item.RepresentationId != id)
                .OrderBy(item => item.JoinedAtUtc)
                .Select(item => item.Representation.Name)
                .FirstOrDefaultAsync(cancellationToken);
            user.Representation = nextRepresentationName;
            user.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (user is not null)
        {
            await _authSessionService.RefreshUserSessionsAsync(user, cancellationToken);
        }

        return NoContent();
    }

    private async Task<IReadOnlyList<RepresentationResponse>> BuildResponsesAsync(
        IReadOnlyList<Representation> representations,
        int currentUserId,
        CancellationToken cancellationToken)
    {
        if (representations.Count == 0)
        {
            return [];
        }

        var representationIds = representations.Select(representation => representation.Id).ToHashSet();
        var memberships = await _dbContext.RepresentationMemberships
            .AsNoTracking()
            .Where(membership => representationIds.Contains(membership.RepresentationId))
            .Select(membership => new
            {
                membership.RepresentationId,
                membership.UserId,
                membership.Role,
                membership.User.Rating,
            })
            .ToListAsync(cancellationToken);

        var memberUserIds = memberships.Select(membership => membership.UserId).Distinct().ToHashSet();
        var submissionRows = await _dbContext.ExerciseSubmissions
            .AsNoTracking()
            .Where(submission => memberUserIds.Contains(submission.UserId))
            .Select(submission => new { submission.UserId, submission.ExerciseId, submission.Status })
            .ToListAsync(cancellationToken);

        var totalTheoryPages = await _dbContext.TheoryContents
            .AsNoTracking()
            .SumAsync(content => (int?)content.PageCount, cancellationToken) ?? 0;

        var readRows = await _dbContext.TheoryPageReadProgresses
            .AsNoTracking()
            .Where(progress =>
                totalTheoryPages > 0 &&
                memberUserIds.Contains(progress.UserId) &&
                progress.ContentVersion == progress.TheoryContent.Version)
            .Select(progress => new { progress.UserId, progress.TheoryContentId, progress.PageIndex })
            .Distinct()
            .ToListAsync(cancellationToken);

        return representations
            .Select(representation =>
            {
                var representationMembers = memberships
                    .Where(membership => membership.RepresentationId == representation.Id)
                    .ToList();
                var userIds = representationMembers.Select(membership => membership.UserId).ToHashSet();
                var totalRating = representationMembers.Sum(membership => membership.Rating);
                var submissionCount = submissionRows.Count(submission => userIds.Contains(submission.UserId));
                var solved = submissionRows
                    .Where(submission => userIds.Contains(submission.UserId) && submission.Status == SubmissionStatus.Passed)
                    .Select(submission => new { submission.UserId, submission.ExerciseId })
                    .Distinct()
                    .Count();
                var readPageCount = readRows.Count(row => userIds.Contains(row.UserId));
                var theoryTotal = totalTheoryPages * Math.Max(userIds.Count, 0);
                var currentMembership = representationMembers.FirstOrDefault(membership => membership.UserId == currentUserId);

                return new RepresentationResponse
                {
                    Id = representation.Id,
                    Name = representation.Name,
                    Description = representation.Description,
                    MemberCount = representationMembers.Count,
                    TotalRating = totalRating,
                    AverageRating = CalculatePercent(totalRating, representationMembers.Count, roundToInteger: true),
                    TheoryProgressPercent = CalculatePercent(readPageCount, theoryTotal),
                    ExerciseSolved = solved,
                    ExerciseSubmissionCount = submissionCount,
                    IsMember = currentMembership is not null,
                    IsOwner = currentMembership?.Role == RepresentationMemberRole.Owner,
                    CreatedAtUtc = representation.CreatedAtUtc,
                };
            })
            .ToList();
    }

    private int? GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("nameid");

        return int.TryParse(userIdValue, out var userId) ? userId : null;
    }

    private static int CalculatePercent(int value, int total, bool roundToInteger = false)
    {
        if (total <= 0)
        {
            return 0;
        }

        return roundToInteger
            ? (int)Math.Round(value * 1.0 / total)
            : (int)Math.Round(value * 100.0 / total);
    }

    private static string NormalizeName(string name) => name.Trim().ToLowerInvariant();
}
