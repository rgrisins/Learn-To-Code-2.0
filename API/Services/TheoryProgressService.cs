using System.Text.Json;
using LearnToCode.API.Contracts.Theory;
using LearnToCode.Data;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace LearnToCode.API.Services;

public class TheoryProgressService
{
    private const string CachePrefix = "learn-to-code:theory-progress:";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);

    private readonly AppDbContext _dbContext;
    private readonly IConnectionMultiplexer? _redis;
    private readonly IConfiguration _configuration;

    public TheoryProgressService(
        AppDbContext dbContext,
        IEnumerable<IConnectionMultiplexer> redisConnections,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _redis = redisConnections.FirstOrDefault();
        _configuration = configuration;
    }

    public async Task<IReadOnlyDictionary<string, int>> GetLanguageProgressPercentsAsync(int userId, CancellationToken cancellationToken)
    {
        var cached = await TryReadCacheAsync<Dictionary<string, int>>(await BuildUserCacheKeyAsync(userId, "languages", cancellationToken), cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var snapshots = await GetTopicSnapshotsAsync(null, cancellationToken);
        var progress = await BuildLanguageProgressAsync(userId, snapshots, cancellationToken);

        await TryWriteCacheAsync(await BuildUserCacheKeyAsync(userId, "languages", cancellationToken), progress, cancellationToken);
        return progress;
    }

    public async Task<IReadOnlyDictionary<string, int>> GetTopicProgressPercentsAsync(int userId, string languageCode, CancellationToken cancellationToken)
    {
        var normalizedLanguageCode = languageCode.Trim().ToLowerInvariant();
        var cached = await TryReadCacheAsync<Dictionary<string, int>>(await BuildUserCacheKeyAsync(userId, $"language:{normalizedLanguageCode}:topics", cancellationToken), cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var snapshots = await GetTopicSnapshotsAsync(normalizedLanguageCode, cancellationToken);
        var readCounts = await GetReadPageCountsAsync(userId, snapshots, cancellationToken);
        var quizCorrectCounts = await GetQuizCorrectCountsAsync(userId, snapshots, cancellationToken);
        var progress = snapshots.ToDictionary(
            snapshot => snapshot.TopicSlug,
            snapshot => CalculateTopicPercent(snapshot, readCounts, quizCorrectCounts),
            StringComparer.OrdinalIgnoreCase);

        await TryWriteCacheAsync(await BuildUserCacheKeyAsync(userId, $"language:{normalizedLanguageCode}:topics", cancellationToken), progress, cancellationToken);
        return progress;
    }

    public async Task<TheoryProgressUpdateResponse?> MarkPageReadAsync(
        int userId,
        string languageCode,
        string topicId,
        int page,
        CancellationToken cancellationToken)
    {
        var snapshot = await GetTopicSnapshotAsync(languageCode, topicId, cancellationToken);

        if (snapshot is null || page < 1 || page > snapshot.PageCount)
        {
            return null;
        }

        var existing = await _dbContext.TheoryPageReadProgresses
            .FirstOrDefaultAsync(progress =>
                    progress.UserId == userId &&
                    progress.TheoryContentId == snapshot.ContentId &&
                    progress.ContentVersion == snapshot.ContentVersion &&
                    progress.PageIndex == page,
                cancellationToken);

        var changed = false;
        if (existing is null)
        {
            _dbContext.TheoryPageReadProgresses.Add(new TheoryPageReadProgress
            {
                UserId = userId,
                TheoryContentId = snapshot.ContentId,
                ContentVersion = snapshot.ContentVersion,
                PageIndex = page,
                ReadAtUtc = DateTime.UtcNow,
            });
            changed = true;
        }
        else
        {
            existing.ReadAtUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (changed)
        {
            await InvalidateUserProgressCacheAsync(userId, cancellationToken);
        }

        var languageProgress = await GetLanguageProgressPercentsAsync(userId, cancellationToken);
        var topicProgress = await GetTopicProgressPercentsAsync(userId, snapshot.LanguageCode, cancellationToken);

        return new TheoryProgressUpdateResponse
        {
            LanguageId = snapshot.LanguageCode,
            TopicId = snapshot.TopicSlug,
            PageIndex = page,
            IsRead = true,
            LanguageProgressPercent = languageProgress.GetValueOrDefault(snapshot.LanguageCode),
            TopicProgressPercent = topicProgress.GetValueOrDefault(snapshot.TopicSlug),
        };
    }

    public async Task<IReadOnlyList<int>> GetReadPageIndicesAsync(
        int userId, string languageCode, string topicId, CancellationToken cancellationToken)
    {
        var snapshot = await GetTopicSnapshotAsync(languageCode, topicId, cancellationToken);

        if (snapshot is null) return [];

        return await _dbContext.TheoryPageReadProgresses
            .AsNoTracking()
            .Where(p =>
                p.UserId == userId &&
                p.TheoryContentId == snapshot.ContentId &&
                p.ContentVersion == snapshot.ContentVersion)
            .Select(p => p.PageIndex)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsPageReadAsync(int userId, string languageCode, string topicId, int page, CancellationToken cancellationToken)
    {
        var snapshot = await GetTopicSnapshotAsync(languageCode, topicId, cancellationToken);

        if (snapshot is null)
        {
            return false;
        }

        return await _dbContext.TheoryPageReadProgresses
            .AsNoTracking()
            .AnyAsync(progress =>
                    progress.UserId == userId &&
                    progress.TheoryContentId == snapshot.ContentId &&
                    progress.ContentVersion == snapshot.ContentVersion &&
                    progress.PageIndex == page,
                cancellationToken);
    }

    public async Task InvalidateUserProgressCacheAsync(int userId, CancellationToken cancellationToken)
    {
        var database = GetDatabaseOrNull();
        if (database is null)
        {
            return;
        }

        try
        {
            await database.StringIncrementAsync(GetUserVersionKey(userId)).WaitAsync(cancellationToken);
        }
        catch
        {
            // Progress joprojām tiek saglabāts datubāzē arī bez Redis keša.
        }
    }

    public async Task InvalidateAllProgressCacheAsync(CancellationToken cancellationToken)
    {
        var database = GetDatabaseOrNull();
        if (database is null)
        {
            return;
        }

        try
        {
            await database.StringIncrementAsync(GetGlobalVersionKey()).WaitAsync(cancellationToken);
        }
        catch
        {
            // Progress joprojām tiek saglabāts datubāzē arī bez Redis keša.
        }
    }

    private async Task<Dictionary<string, int>> BuildLanguageProgressAsync(
        int userId,
        IReadOnlyList<TopicProgressSnapshot> snapshots,
        CancellationToken cancellationToken)
    {
        var readCounts = await GetReadPageCountsAsync(userId, snapshots, cancellationToken);
        var quizCorrectCounts = await GetQuizCorrectCountsAsync(userId, snapshots, cancellationToken);

        return snapshots
            .GroupBy(snapshot => snapshot.LanguageCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var total = 0;
                    var done = 0;
                    foreach (var snapshot in group)
                    {
                        total += snapshot.PageCount + snapshot.QuizQuestionCount;
                        done += Math.Min(readCounts.GetValueOrDefault(snapshot.ContentId), snapshot.PageCount);
                        if (snapshot.QuizId is int quizId)
                        {
                            done += Math.Min(quizCorrectCounts.GetValueOrDefault(quizId), snapshot.QuizQuestionCount);
                        }
                    }

                    return CalculatePercent(done, total);
                },
                StringComparer.OrdinalIgnoreCase);
    }

    private async Task<IReadOnlyList<TopicProgressSnapshot>> GetTopicSnapshotsAsync(string? languageCode, CancellationToken cancellationToken)
    {
        var normalizedLanguageCode = languageCode?.Trim().ToLowerInvariant();

        var rows = await _dbContext.TheoryTopics
            .AsNoTracking()
            .Where(topic =>
                topic.Content != null &&
                topic.Language != null &&
                (normalizedLanguageCode == null || topic.Language.Title.ToLower() == normalizedLanguageCode))
            .Select(topic => new
            {
                LanguageCode = topic.Language!.Title,
                TopicTitle = topic.Title,
                ContentId = topic.Content!.Id,
                topic.Content.Version,
                topic.Content.PageCount,
                QuizId = (int?)(topic.Quiz != null ? topic.Quiz.Id : (int?)null),
                QuizQuestionCount = topic.Quiz != null ? topic.Quiz.Questions.Count : 0,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(topic => new TopicProgressSnapshot(
                topic.LanguageCode,
                TheoryTopicKey.FromTitle(topic.TopicTitle),
                topic.ContentId,
                topic.Version,
                topic.PageCount,
                topic.QuizId,
                topic.QuizQuestionCount))
            .ToList();
    }

    private async Task<TopicProgressSnapshot?> GetTopicSnapshotAsync(
        string languageCode,
        string topicId,
        CancellationToken cancellationToken)
    {
        var normalizedLanguageCode = languageCode.Trim().ToLowerInvariant();
        var normalizedTopicId = TheoryTopicKey.Normalize(topicId);

        var rows = await _dbContext.TheoryTopics
            .AsNoTracking()
            .Where(topic =>
                topic.Content != null &&
                topic.Language != null &&
                topic.Language.Title.ToLower() == normalizedLanguageCode)
            .Select(topic => new
            {
                LanguageCode = topic.Language!.Title,
                TopicTitle = topic.Title,
                ContentId = topic.Content!.Id,
                topic.Content.Version,
                topic.Content.PageCount,
                QuizId = (int?)(topic.Quiz != null ? topic.Quiz.Id : (int?)null),
                QuizQuestionCount = topic.Quiz != null ? topic.Quiz.Questions.Count : 0,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(topic => new TopicProgressSnapshot(
                topic.LanguageCode,
                TheoryTopicKey.FromTitle(topic.TopicTitle),
                topic.ContentId,
                topic.Version,
                topic.PageCount,
                topic.QuizId,
                topic.QuizQuestionCount))
            .FirstOrDefault(topic => string.Equals(topic.TopicSlug, normalizedTopicId, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<Dictionary<int, int>> GetReadPageCountsAsync(
        int userId,
        IReadOnlyList<TopicProgressSnapshot> snapshots,
        CancellationToken cancellationToken)
    {
        if (snapshots.Count == 0)
        {
            return [];
        }

        var contentIds = snapshots.Select(snapshot => snapshot.ContentId).ToHashSet();
        var versions = snapshots.ToDictionary(snapshot => snapshot.ContentId, snapshot => snapshot.ContentVersion);

        var rows = await _dbContext.TheoryPageReadProgresses
            .AsNoTracking()
            .Where(progress => progress.UserId == userId && contentIds.Contains(progress.TheoryContentId))
            .Select(progress => new { progress.TheoryContentId, progress.ContentVersion, progress.PageIndex })
            .ToListAsync(cancellationToken);

        return rows
            .Where(row => versions.GetValueOrDefault(row.TheoryContentId) == row.ContentVersion)
            .GroupBy(row => row.TheoryContentId)
            .ToDictionary(group => group.Key, group => group.Select(row => row.PageIndex).Distinct().Count());
    }

    private async Task<Dictionary<int, int>> GetQuizCorrectCountsAsync(
        int userId,
        IReadOnlyList<TopicProgressSnapshot> snapshots,
        CancellationToken cancellationToken)
    {
        var quizIds = snapshots
            .Where(snapshot => snapshot.QuizId.HasValue)
            .Select(snapshot => snapshot.QuizId!.Value)
            .ToHashSet();

        if (quizIds.Count == 0)
        {
            return [];
        }

        var rows = await _dbContext.TheoryQuizQuestionAnswers
            .AsNoTracking()
            .Where(answer =>
                answer.UserId == userId &&
                answer.IsCorrect &&
                answer.Question != null &&
                quizIds.Contains(answer.Question.QuizId))
            .Select(answer => new { QuizId = answer.Question!.QuizId, answer.QuestionId })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.QuizId)
            .ToDictionary(group => group.Key, group => group.Select(row => row.QuestionId).Distinct().Count());
    }

    private async Task<T?> TryReadCacheAsync<T>(string key, CancellationToken cancellationToken)
    {
        var database = GetDatabaseOrNull();
        if (database is null)
        {
            return default;
        }

        try
        {
            var value = await database.StringGetAsync(key).WaitAsync(cancellationToken);
            return value.HasValue ? JsonSerializer.Deserialize<T>(value.ToString()) : default;
        }
        catch
        {
            return default;
        }
    }

    private async Task TryWriteCacheAsync<T>(string key, T payload, CancellationToken cancellationToken)
    {
        var database = GetDatabaseOrNull();
        if (database is null)
        {
            return;
        }

        try
        {
            await database.StringSetAsync(key, JsonSerializer.Serialize(payload), CacheTtl).WaitAsync(cancellationToken);
        }
        catch
        {
            // Keša kļūda šeit nedrīkst apturēt progresa aprēķinu.
        }
    }

    // Keša atslēgā iekļauj versijas, lai pēc satura vai lietotāja progresa izmaiņām dati pārrēķinātos.
    private async Task<string> BuildUserCacheKeyAsync(int userId, string suffix, CancellationToken cancellationToken)
    {
        var globalVersion = await GetCacheVersionAsync(GetGlobalVersionKey(), cancellationToken);
        var userVersion = await GetCacheVersionAsync(GetUserVersionKey(userId), cancellationToken);
        return $"{CachePrefix}cache:g:{globalVersion}:u:{userId}:{userVersion}:{suffix}";
    }

    private async Task<long> GetCacheVersionAsync(string key, CancellationToken cancellationToken)
    {
        var database = GetDatabaseOrNull();
        if (database is null)
        {
            return 0;
        }

        try
        {
            var value = await database.StringGetAsync(key).WaitAsync(cancellationToken);
            return value.HasValue && long.TryParse(value.ToString(), out var version) ? version : 0;
        }
        catch
        {
            return 0;
        }
    }

    private IDatabase? GetDatabaseOrNull()
    {
        if (_redis is null)
        {
            return null;
        }

        return _redis.GetDatabase(GetDatabaseIndex());
    }

    private int GetDatabaseIndex() =>
        int.TryParse(_configuration["Redis:Database"], out var database) ? database : 0;

    private static int CalculatePercent(int readPages, int totalPages) =>
        totalPages <= 0 ? 0 : (int)Math.Round(readPages * 100.0 / totalPages);

    private static int CalculateTopicPercent(
        TopicProgressSnapshot snapshot,
        IReadOnlyDictionary<int, int> readCounts,
        IReadOnlyDictionary<int, int> quizCorrectCounts)
    {
        var total = snapshot.PageCount + snapshot.QuizQuestionCount;
        if (total <= 0) return 0;

        var done = Math.Min(readCounts.GetValueOrDefault(snapshot.ContentId), snapshot.PageCount);
        if (snapshot.QuizId is int quizId)
        {
            done += Math.Min(quizCorrectCounts.GetValueOrDefault(quizId), snapshot.QuizQuestionCount);
        }

        return CalculatePercent(done, total);
    }

    private static string GetGlobalVersionKey() => $"{CachePrefix}global-version";

    private static string GetUserVersionKey(int userId) => $"{CachePrefix}user-version:{userId}";

    private sealed record TopicProgressSnapshot(
        string LanguageCode,
        string TopicSlug,
        int ContentId,
        int ContentVersion,
        int PageCount,
        int? QuizId,
        int QuizQuestionCount);
}
