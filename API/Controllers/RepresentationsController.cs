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

    [HttpGet("by-name/{name}")]
    public async Task<ActionResult<RepresentationResponse>> GetByName(string name, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var normalized = NormalizeName(name);
        if (string.IsNullOrEmpty(normalized))
        {
            return NotFound(new { message = "Pārstāvniecība nav atrasta." });
        }

        var representation = await _dbContext.Representations
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.NormalizedName == normalized, cancellationToken);

        if (representation is null)
        {
            return NotFound(new { message = "Pārstāvniecība nav atrasta." });
        }

        var responses = await BuildResponsesAsync([representation], userId.Value, cancellationToken);
        return Ok(responses.Single());
    }

    [HttpGet("{id:int}/members")]
    public async Task<ActionResult<IEnumerable<RepresentationMemberResponse>>> GetMembers(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        // Jebkurš autorizēts lietotājs drīkst redzēt pārstāvniecības dalībnieku
        // sarakstu — tas ir publisks reitinga uzskaitījums, ne tikai privāts saraksts.
        var representationExists = await _dbContext.Representations
            .AsNoTracking()
            .AnyAsync(representation => representation.Id == id, cancellationToken);

        if (!representationExists)
        {
            return NotFound(new { message = "Pārstāvniecība nav atrasta." });
        }

        // FullName ir aprēķināma īpašība, tāpēc ielādējam pilnās entītes un mapojam C# atmiņā.
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

        // Viens lietotājs drīkst būt tikai vienā pārstāvniecībā vienlaikus.
        var alreadyInRepresentation = await _dbContext.RepresentationMemberships
            .AnyAsync(membership => membership.UserId == userId.Value, cancellationToken);

        if (alreadyInRepresentation)
        {
            return Conflict(new { message = "Tu jau esi citā pārstāvniecībā. Vispirms izstājies no esošās." });
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
            IsPublic = request.IsPublic,
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

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RepresentationResponse>> Update(int id, [FromBody] RepresentationCreateRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var representation = await _dbContext.Representations
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (representation is null)
        {
            return NotFound(new { message = "Pārstāvniecība nav atrasta." });
        }

        var isAdministrator = User.IsInRole(nameof(UserRole.Administrators));
        if (!isAdministrator && !await IsOwnerAsync(id, userId.Value, cancellationToken))
        {
            return Forbid();
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

        if (representation.NormalizedName != normalizedName)
        {
            var nameTaken = await _dbContext.Representations
                .AnyAsync(item => item.Id != id && item.NormalizedName == normalizedName, cancellationToken);

            if (nameTaken)
            {
                return Conflict(new { message = "Pārstāvniecība ar šādu nosaukumu jau eksistē." });
            }
        }

        var oldName = representation.Name;
        representation.Name = name;
        representation.NormalizedName = normalizedName;
        representation.Description = description;
        representation.IsPublic = request.IsPublic;

        // Sinhronizējam User.Representation laukus, kas atspoguļo iepriekšējo nosaukumu.
        if (!string.Equals(oldName, name, StringComparison.Ordinal))
        {
            var memberUserIds = await _dbContext.RepresentationMemberships
                .Where(membership => membership.RepresentationId == id)
                .Select(membership => membership.UserId)
                .ToListAsync(cancellationToken);

            var usersToUpdate = await _dbContext.Users
                .Where(user => memberUserIds.Contains(user.Id) && user.Representation == oldName)
                .ToListAsync(cancellationToken);

            var nowUtc = DateTime.UtcNow;
            foreach (var user in usersToUpdate)
            {
                user.Representation = name;
                user.UpdatedAtUtc = nowUtc;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var responses = await BuildResponsesAsync([representation], userId.Value, cancellationToken);
        return Ok(responses.Single());
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (!User.IsInRole(nameof(UserRole.Administrators)))
        {
            return Forbid();
        }

        var representation = await _dbContext.Representations
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (representation is null)
        {
            return NotFound(new { message = "Pārstāvniecība nav atrasta." });
        }

        var usersToUpdate = await _dbContext.Users
            .Where(user => user.Representation == representation.Name)
            .ToListAsync(cancellationToken);

        var nowUtc = DateTime.UtcNow;
        foreach (var user in usersToUpdate)
        {
            user.Representation = null;
            user.UpdatedAtUtc = nowUtc;
        }

        _dbContext.Representations.Remove(representation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var user in usersToUpdate)
        {
            await _authSessionService.RefreshUserSessionsAsync(user, cancellationToken);
        }

        return NoContent();
    }

    [HttpPost("{id:int}/join")]
    public async Task<ActionResult<object>> Join(int id, [FromBody] RepresentationJoinRequestCreateRequest? body, CancellationToken cancellationToken)
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

        var existingMembership = await _dbContext.RepresentationMemberships
            .FirstOrDefaultAsync(membership => membership.UserId == userId.Value, cancellationToken);

        if (existingMembership is not null && existingMembership.RepresentationId == id)
        {
            // Jau dalībnieks — vienkārši atgriežam pārstāvniecības atbildi.
            var responses = await BuildResponsesAsync([representation], userId.Value, cancellationToken);
            return Ok(new { membership = true, representation = responses.Single() });
        }

        if (existingMembership is not null && existingMembership.RepresentationId != id)
        {
            return Conflict(new { message = "Tu jau esi citā pārstāvniecībā. Vispirms izstājies no esošās." });
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(existingUser => existingUser.Id == userId.Value, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        // Privātās pārstāvniecības — tā vietā, lai uzreiz iestātos, izveidojam
        // pieprasījumu, ko apstiprina īpašnieks vai moderators.
        if (!representation.IsPublic)
        {
            var existingRequest = await _dbContext.RepresentationJoinRequests
                .FirstOrDefaultAsync(request =>
                    request.RepresentationId == id &&
                    request.UserId == userId.Value &&
                    request.Status == RepresentationJoinRequestStatus.Pending,
                    cancellationToken);

            if (existingRequest is not null)
            {
                return Conflict(new { message = "Pieprasījums jau ir iesniegts un gaida izskatīšanu." });
            }

            var trimmedMessage = string.IsNullOrWhiteSpace(body?.Message)
                ? null
                : body!.Message!.Trim();
            if (trimmedMessage is { Length: > 500 })
            {
                trimmedMessage = trimmedMessage[..500];
            }

            _dbContext.RepresentationJoinRequests.Add(new RepresentationJoinRequest
            {
                RepresentationId = id,
                UserId = userId.Value,
                Status = RepresentationJoinRequestStatus.Pending,
                Message = trimmedMessage,
                CreatedAtUtc = DateTime.UtcNow,
            });

            await _dbContext.SaveChangesAsync(cancellationToken);

            return Accepted(new { pendingRequest = true, message = "Pieprasījums nosūtīts īpašniekam." });
        }

        // Publiskā pārstāvniecība — uzreiz iestājamies.
        _dbContext.RepresentationMemberships.Add(new RepresentationMembership
        {
            RepresentationId = representation.Id,
            UserId = user.Id,
            Role = RepresentationMemberRole.Member,
            JoinedAtUtc = DateTime.UtcNow,
        });
        await DeletePendingJoinRequestsForUserAsync(user.Id, cancellationToken);

        if (string.IsNullOrWhiteSpace(user.Representation))
        {
            user.Representation = representation.Name;
            user.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _authSessionService.RefreshUserSessionsAsync(user, cancellationToken);

        var joinedResponses = await BuildResponsesAsync([representation], user.Id, cancellationToken);
        return Ok(new { membership = true, representation = joinedResponses.Single() });
    }

    // Pievienošanās pieprasījumu pārvaldība īpašniekam un moderatoram.

    [HttpGet("{id:int}/requests")]
    public async Task<ActionResult<IEnumerable<RepresentationJoinRequestResponse>>> GetJoinRequests(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var canManage = await CanManageAsync(id, userId.Value, cancellationToken);
        if (!canManage) return Forbid();

        var requests = await _dbContext.RepresentationJoinRequests
            .AsNoTracking()
            .Include(request => request.User)
            .Where(request =>
                request.RepresentationId == id &&
                request.Status == RepresentationJoinRequestStatus.Pending)
            .OrderBy(request => request.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var responses = requests.Select(request => new RepresentationJoinRequestResponse
        {
            Id = request.Id,
            UserId = request.UserId,
            Username = request.User?.Username,
            FullName = request.User?.FullName ?? string.Empty,
            UserRating = request.User?.Rating ?? 0,
            Message = request.Message,
            CreatedAtUtc = request.CreatedAtUtc,
        }).ToList();

        return Ok(responses);
    }

    [HttpPost("{id:int}/requests/{requestId:int}/approve")]
    public async Task<IActionResult> ApproveJoinRequest(int id, int requestId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var canManage = await CanManageAsync(id, userId.Value, cancellationToken);
        if (!canManage) return Forbid();

        var request = await _dbContext.RepresentationJoinRequests
            .Include(item => item.User)
            .Include(item => item.Representation)
            .FirstOrDefaultAsync(item =>
                item.Id == requestId &&
                item.RepresentationId == id &&
                item.Status == RepresentationJoinRequestStatus.Pending,
                cancellationToken);

        if (request is null) return NotFound(new { message = "Pieprasījums nav atrasts." });

        // Ja pieprasītājs jau ir citā pārstāvniecībā, atsakām un atzīmējam.
        var hasOtherMembership = await _dbContext.RepresentationMemberships
            .AnyAsync(membership => membership.UserId == request.UserId && membership.RepresentationId != id, cancellationToken);

        if (hasOtherMembership)
        {
            _dbContext.RepresentationJoinRequests.Remove(request);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Conflict(new { message = "Lietotājs jau ir citā pārstāvniecībā." });
        }

        // Ja lietotājs jau ir dalībnieks vienlaicīgas darbības dēļ, tikai atzīmējam apstiprinātu.
        var alreadyMember = await _dbContext.RepresentationMemberships
            .AnyAsync(membership => membership.RepresentationId == id && membership.UserId == request.UserId, cancellationToken);

        if (!alreadyMember)
        {
            _dbContext.RepresentationMemberships.Add(new RepresentationMembership
            {
                RepresentationId = id,
                UserId = request.UserId,
                Role = RepresentationMemberRole.Member,
                JoinedAtUtc = DateTime.UtcNow,
            });

            if (request.User is not null && string.IsNullOrWhiteSpace(request.User.Representation))
            {
                request.User.Representation = request.Representation.Name;
                request.User.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        request.Status = RepresentationJoinRequestStatus.Approved;
        request.ResolvedAtUtc = DateTime.UtcNow;
        request.ResolvedByUserId = userId.Value;
        await DeletePendingJoinRequestsForUserAsync(request.UserId, cancellationToken, request.Id);

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (request.User is not null)
        {
            await _authSessionService.RefreshUserSessionsAsync(request.User, cancellationToken);
        }

        return NoContent();
    }

    [HttpPost("{id:int}/requests/{requestId:int}/reject")]
    public async Task<IActionResult> RejectJoinRequest(int id, int requestId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var canManage = await CanManageAsync(id, userId.Value, cancellationToken);
        if (!canManage) return Forbid();

        var request = await _dbContext.RepresentationJoinRequests
            .FirstOrDefaultAsync(item =>
                item.Id == requestId &&
                item.RepresentationId == id &&
                item.Status == RepresentationJoinRequestStatus.Pending,
                cancellationToken);

        if (request is null) return NotFound(new { message = "Pieprasījums nav atrasts." });

        request.Status = RepresentationJoinRequestStatus.Rejected;
        request.ResolvedAtUtc = DateTime.UtcNow;
        request.ResolvedByUserId = userId.Value;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // Dalībnieku izņemšana un lomu maiņa pārstāvniecībā.

    [HttpDelete("{id:int}/members/{memberUserId:int}")]
    public async Task<IActionResult> KickMember(int id, int memberUserId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var actorMembership = await _dbContext.RepresentationMemberships
            .AsNoTracking()
            .FirstOrDefaultAsync(membership =>
                membership.RepresentationId == id && membership.UserId == userId.Value,
                cancellationToken);

        if (actorMembership is null) return Forbid();
        if (actorMembership.Role == RepresentationMemberRole.Member)
        {
            return Forbid();
        }

        if (memberUserId == userId.Value)
        {
            return BadRequest(new { message = "Nevar izmest sevi — izmanto Izstāties." });
        }

        var target = await _dbContext.RepresentationMemberships
            .Include(membership => membership.Representation)
            .FirstOrDefaultAsync(membership =>
                membership.RepresentationId == id && membership.UserId == memberUserId,
                cancellationToken);

        if (target is null) return NotFound(new { message = "Dalībnieks nav atrasts." });

        // Īpašnieks var izmest jebkuru. Moderators drīkst izmest tikai parastos
        // dalībniekus, bet ne citus moderatorus vai īpašnieku.
        if (actorMembership.Role == RepresentationMemberRole.Moderators &&
            target.Role != RepresentationMemberRole.Member)
        {
            return Forbid();
        }

        if (target.Role == RepresentationMemberRole.Owner)
        {
            return BadRequest(new { message = "Owner nevar tikt izmests. Vispirms nodod īpašumtiesības." });
        }

        var targetUser = await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == memberUserId, cancellationToken);
        var representationName = target.Representation.Name;

        _dbContext.RepresentationMemberships.Remove(target);

        if (targetUser is not null && string.Equals(targetUser.Representation, representationName, StringComparison.OrdinalIgnoreCase))
        {
            targetUser.Representation = null;
            targetUser.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (targetUser is not null)
        {
            await _authSessionService.RefreshUserSessionsAsync(targetUser, cancellationToken);
        }

        return NoContent();
    }

    [HttpPut("{id:int}/members/{memberUserId:int}/role")]
    public async Task<IActionResult> UpdateMemberRole(int id, int memberUserId, [FromBody] RepresentationMemberRoleUpdateRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        // Tikai īpašnieks var mainīt lomas.
        var actorMembership = await _dbContext.RepresentationMemberships
            .AsNoTracking()
            .FirstOrDefaultAsync(membership =>
                membership.RepresentationId == id &&
                membership.UserId == userId.Value &&
                membership.Role == RepresentationMemberRole.Owner,
                cancellationToken);

        if (actorMembership is null) return Forbid();

        if (memberUserId == userId.Value)
        {
            return BadRequest(new { message = "Nevar mainīt savu lomu." });
        }

        if (!Enum.TryParse<RepresentationMemberRole>(request.Role, ignoreCase: true, out var newRole))
        {
            return BadRequest(new { message = "Nederīga loma." });
        }

        // Īpašnieka lomu nevar piešķirt caur šo galapunktu, jo tam vajadzīga
        // atsevišķa īpašumtiesību nodošanas loģika.
        if (newRole == RepresentationMemberRole.Owner)
        {
            return BadRequest(new { message = "Owner pārcelšana pagaidām nav atbalstīta." });
        }

        var target = await _dbContext.RepresentationMemberships
            .FirstOrDefaultAsync(membership =>
                membership.RepresentationId == id && membership.UserId == memberUserId,
                cancellationToken);

        if (target is null) return NotFound(new { message = "Dalībnieks nav atrasts." });

        if (target.Role == RepresentationMemberRole.Owner)
        {
            return BadRequest(new { message = "Owner lomu nevar mainīt caur šo darbību." });
        }

        target.Role = newRole;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<bool> CanManageAsync(int representationId, int userId, CancellationToken cancellationToken)
    {
        return await _dbContext.RepresentationMemberships
            .AsNoTracking()
            .AnyAsync(membership =>
                membership.RepresentationId == representationId &&
                membership.UserId == userId &&
                (membership.Role == RepresentationMemberRole.Owner ||
                 membership.Role == RepresentationMemberRole.Moderators),
                cancellationToken);
    }

    private async Task<bool> IsOwnerAsync(int representationId, int userId, CancellationToken cancellationToken)
    {
        return await _dbContext.RepresentationMemberships
            .AsNoTracking()
            .AnyAsync(membership =>
                membership.RepresentationId == representationId &&
                membership.UserId == userId &&
                membership.Role == RepresentationMemberRole.Owner,
                cancellationToken);
    }

    private async Task DeletePendingJoinRequestsForUserAsync(int userId, CancellationToken cancellationToken, int? exceptRequestId = null)
    {
        var query = _dbContext.RepresentationJoinRequests
            .Where(request =>
                request.UserId == userId &&
                request.Status == RepresentationJoinRequestStatus.Pending);

        if (exceptRequestId is int requestId)
        {
            query = query.Where(request => request.Id != requestId);
        }

        var pendingRequests = await query.ToListAsync(cancellationToken);
        _dbContext.RepresentationJoinRequests.RemoveRange(pendingRequests);
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
        var sevenDaysAgoUtc = DateTime.UtcNow.AddDays(-7);

        var pendingRequestRows = await _dbContext.RepresentationJoinRequests
            .AsNoTracking()
            .Where(request =>
                representationIds.Contains(request.RepresentationId) &&
                request.Status == RepresentationJoinRequestStatus.Pending)
            .Select(request => new { request.RepresentationId, request.UserId })
            .ToListAsync(cancellationToken);
        var submissionRows = await _dbContext.ExerciseSubmissions
            .AsNoTracking()
            .Where(submission => memberUserIds.Contains(submission.UserId))
            .Select(submission => new
            {
                submission.UserId,
                submission.ExerciseId,
                submission.Status,
                submission.SubmittedAtUtc,
            })
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
                // Pēdējās 7 dienās: skaitām unikālus (lietotājs, uzdevums) pārus, kuriem
            // pirmais veiksmīgais iesūtījums (Passed) ir noticis šajā logā.
                var solvedLast7Days = submissionRows
                    .Where(submission => userIds.Contains(submission.UserId)
                                          && submission.Status == SubmissionStatus.Passed
                                          && submission.SubmittedAtUtc >= sevenDaysAgoUtc)
                    .GroupBy(submission => new { submission.UserId, submission.ExerciseId })
                    .Count();
                var readPageCount = readRows.Count(row => userIds.Contains(row.UserId));
                var theoryTotal = totalTheoryPages * Math.Max(userIds.Count, 0);
                var currentMembership = representationMembers.FirstOrDefault(membership => membership.UserId == currentUserId);
                var pendingForRep = pendingRequestRows.Where(row => row.RepresentationId == representation.Id).ToList();
                var hasPendingRequestForCurrentUser = pendingForRep.Any(row => row.UserId == currentUserId);

                return new RepresentationResponse
                {
                    Id = representation.Id,
                    Name = representation.Name,
                    Description = representation.Description,
                    IsPublic = representation.IsPublic,
                    MemberCount = representationMembers.Count,
                    TotalRating = totalRating,
                    AverageRating = CalculatePercent(totalRating, representationMembers.Count, roundToInteger: true),
                    TheoryProgressPercent = CalculatePercent(readPageCount, theoryTotal),
                    ExerciseSolved = solved,
                    ExerciseSolvedLast7Days = solvedLast7Days,
                    ExerciseSubmissionCount = submissionCount,
                    IsMember = currentMembership is not null,
                    IsOwner = currentMembership?.Role == RepresentationMemberRole.Owner,
                    IsModerator = currentMembership?.Role == RepresentationMemberRole.Moderators,
                    HasPendingJoinRequest = hasPendingRequestForCurrentUser,
                    PendingJoinRequestCount = (currentMembership?.Role == RepresentationMemberRole.Owner ||
                                                currentMembership?.Role == RepresentationMemberRole.Moderators)
                        ? pendingForRep.Count
                        : 0,
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
