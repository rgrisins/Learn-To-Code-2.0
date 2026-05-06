using LearnToCode.API.Contracts.Ratings;
using LearnToCode.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnToCode.API.Controllers;

[ApiController]
[Route("api/ratings")]
public class RatingsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public RatingsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<RatingUserResponse>>> GetUsers(CancellationToken cancellationToken)
    {
        // FullName ir aprēķināma īpašība, tāpēc ielādējam un mapojam C# atmiņā.
        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Role != UserRole.Administrators)
            .OrderByDescending(user => user.Rating)
            .ThenBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .ToListAsync(cancellationToken);

        return Ok(users.Select(user => new RatingUserResponse
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            Representation = user.Representation,
            Rating = user.Rating,
            CreatedAtUtc = user.CreatedAtUtc,
        }).ToList());
    }

    [HttpGet("representations")]
    public async Task<ActionResult<IEnumerable<RatingRepresentationResponse>>> GetRepresentations(CancellationToken cancellationToken)
    {
        var representations = await _dbContext.Representations
            .AsNoTracking()
            .OrderBy(representation => representation.Name)
            .Select(representation => new
            {
                representation.Id,
                representation.Name,
                representation.Description,
                representation.CreatedAtUtc,
            })
            .ToListAsync(cancellationToken);

        if (representations.Count == 0)
        {
            return Ok(Array.Empty<RatingRepresentationResponse>());
        }

        var representationIds = representations.Select(representation => representation.Id).ToHashSet();
        var memberships = await _dbContext.RepresentationMemberships
            .AsNoTracking()
            .Where(membership => representationIds.Contains(membership.RepresentationId))
            .Select(membership => new
            {
                membership.RepresentationId,
                membership.UserId,
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

        var responses = representations
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
                var theoryTotal = totalTheoryPages * userIds.Count;

                return new RatingRepresentationResponse
                {
                    Id = representation.Id,
                    Name = representation.Name,
                    Description = representation.Description,
                    MemberCount = representationMembers.Count,
                    TotalRating = totalRating,
                    AverageRating = representationMembers.Count == 0
                        ? 0
                        : (int)Math.Round(totalRating * 1.0 / representationMembers.Count),
                    TheoryProgressPercent = CalculatePercent(readPageCount, theoryTotal),
                    ExerciseSolved = solved,
                    ExerciseSubmissionCount = submissionCount,
                    CreatedAtUtc = representation.CreatedAtUtc,
                };
            })
            .OrderByDescending(representation => representation.AverageRating)
            .ThenByDescending(representation => representation.TotalRating)
            .ThenBy(representation => representation.Name)
            .ToList();

        return Ok(responses);
    }

    private static int CalculatePercent(int value, int total) =>
        total <= 0 ? 0 : (int)Math.Round(value * 100.0 / total);
}
