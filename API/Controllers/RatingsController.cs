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
        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Role != UserRole.Administrators)
            .OrderByDescending(user => user.Rating)
            .ThenBy(user => user.FullName)
            .Select(user => new RatingUserResponse
            {
                Id = user.Id,
                Username = user.Username ?? user.NormalizedUsername,
                FullName = user.FullName,
                Role = user.Role.ToString(),
                EducationInstitution = user.EducationInstitution,
                Rating = user.Rating,
                CreatedAtUtc = user.CreatedAtUtc,
            })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }
}
