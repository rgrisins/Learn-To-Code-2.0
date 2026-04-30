using System.Text;
using LearnToCode.API.Contracts.Theory;
using LearnToCode.API.Controllers;
using LearnToCode.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace LearnToCode.API.Services;

public class TheoryCatalogService
{
    private const string PageSeparator = "\n---page---\n";
    private const string SeedVersion = "python-pages-2026-04-28-v2";

    private readonly AppDbContext _dbContext;
    private readonly IHostEnvironment _environment;
    private readonly IMinioClient _minioClient;
    private readonly TheoryStorageOptions _options;
    private readonly TheoryProgressService _progressService;

    public TheoryCatalogService(
        AppDbContext dbContext,
        IHostEnvironment environment,
        IMinioClient minioClient,
        IOptions<TheoryStorageOptions> options,
        TheoryProgressService progressService)
    {
        _dbContext = dbContext;
        _environment = environment;
        _minioClient = minioClient;
        _options = options.Value;
        _progressService = progressService;
    }

    public async Task<IReadOnlyList<TheoryLanguageResponse>> GetLanguagesAsync(int? userId, CancellationToken cancellationToken)
    {
        await SeedDefaultTheoryAsync(cancellationToken);

        var progress = userId.HasValue
            ? await _progressService.GetLanguageProgressPercentsAsync(userId.Value, cancellationToken)
            : new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var languages = await _dbContext.TheoryLanguages
            .AsNoTracking()
            .OrderBy(language => language.SortOrder)
            .ThenBy(language => language.Title)
            .Select(language => new TheoryLanguageResponse
            {
                Id = language.Title,
                Title = language.Title,
                Description = language.Description,
                ImageUrl = GetLanguageImageUrl(language.Title),
                TopicCount = language.Topics.Count(topic => topic.Content != null),
            })
            .ToListAsync(cancellationToken);

        foreach (var language in languages)
        {
            language.ProgressPercent = progress.GetValueOrDefault(language.Id);
        }

        return languages;
    }

    public async Task<TheoryLanguageDetailResponse?> GetLanguageAsync(string languageId, int? userId, CancellationToken cancellationToken)
    {
        await SeedDefaultTheoryAsync(cancellationToken);
        var normalizedLanguageId = languageId.Trim().ToLowerInvariant();

        var language = await _dbContext.TheoryLanguages
            .AsNoTracking()
            .Include(item => item.Topics)
                .ThenInclude(topic => topic.Content)
            .Include(item => item.Topics)
                .ThenInclude(topic => topic.Quiz!)
                    .ThenInclude(quiz => quiz.Questions)
            .FirstOrDefaultAsync(item => item.Title.ToLower() == normalizedLanguageId, cancellationToken);

        if (language is null)
        {
            return null;
        }

        var languageProgress = userId.HasValue
            ? await _progressService.GetLanguageProgressPercentsAsync(userId.Value, cancellationToken)
            : new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var topicProgress = userId.HasValue
            ? await _progressService.GetTopicProgressPercentsAsync(userId.Value, language.Title, cancellationToken)
            : new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var quizQuestionIds = language.Topics
            .Where(topic => topic.Quiz != null)
            .SelectMany(topic => topic.Quiz!.Questions.Select(q => q.Id))
            .ToList();

        var answeredByQuestion = userId.HasValue && quizQuestionIds.Count > 0
            ? (await _dbContext.TheoryQuizQuestionAnswers
                .AsNoTracking()
                .Where(a => a.UserId == userId.Value && quizQuestionIds.Contains(a.QuestionId))
                .Select(a => a.QuestionId)
                .ToListAsync(cancellationToken))
                .ToHashSet()
            : new HashSet<int>();

        var correctByQuestion = userId.HasValue && quizQuestionIds.Count > 0
            ? (await _dbContext.TheoryQuizQuestionAnswers
                .AsNoTracking()
                .Where(a => a.UserId == userId.Value && a.IsCorrect && quizQuestionIds.Contains(a.QuestionId))
                .Select(a => a.QuestionId)
                .ToListAsync(cancellationToken))
                .ToHashSet()
            : new HashSet<int>();

        return new TheoryLanguageDetailResponse
        {
            Id = language.Title,
            Title = language.Title,
            Description = language.Description,
            ImageUrl = GetLanguageImageUrl(language.Title),
            ProgressPercent = languageProgress.GetValueOrDefault(language.Title),
            Topics = language.Topics
                .Where(topic => topic.Content != null)
                .OrderBy(topic => topic.SortOrder)
                .ThenBy(topic => topic.Title)
                .Select(topic =>
                {
                    var topicId = TheoryTopicKey.FromTitle(topic.Title);
                    var quiz = topic.Quiz;
                    var quizQuestionCount = quiz?.Questions.Count ?? 0;
                    var quizAnsweredCount = quiz is null
                        ? 0
                        : quiz.Questions.Count(q => answeredByQuestion.Contains(q.Id));
                    var quizCorrectCount = quiz is null
                        ? 0
                        : quiz.Questions.Count(q => correctByQuestion.Contains(q.Id));

                    return new TheoryTopicResponse
                    {
                        Id = topicId,
                        LanguageId = language.Title,
                        Title = topic.Title,
                        Difficulty = topic.Difficulty,
                        Description = topic.Description,
                        EstimatedMinutes = topic.EstimatedMinutes,
                        PageCount = topic.Content?.PageCount ?? 1,
                        ProgressPercent = topicProgress.GetValueOrDefault(topicId),
                        HasQuiz = quiz is not null && quizQuestionCount > 0,
                        QuizQuestionCount = quizQuestionCount,
                        QuizAnsweredCount = quizAnsweredCount,
                        QuizCorrectAnswerCount = quizCorrectCount,
                    };
                })
                .ToList(),
        };
    }

    public async Task<TheoryPageResponse?> GetPageAsync(string languageId, string topicId, int page, int? userId, CancellationToken cancellationToken)
    {
        await SeedDefaultTheoryAsync(cancellationToken);

        var topic = await FindTopicWithContentAsync(languageId, topicId, asNoTracking: true, cancellationToken);

        if (topic?.Language is null || topic.Content is null)
        {
            return null;
        }

        var resolvedTopicId = TheoryTopicKey.FromTitle(topic.Title);
        var pages = await ReadTopicPagesAsync(topic.Content, cancellationToken);
        if (page < 1 || page > pages.Count)
        {
            return null;
        }

        if (topic.Content.PageCount != pages.Count)
        {
            await UpdatePageCountAsync(topic.Content.Id, pages.Count, cancellationToken);
        }

        var isRead = userId.HasValue &&
            await _progressService.IsPageReadAsync(userId.Value, topic.Language.Title, resolvedTopicId, page, cancellationToken);
        var topicProgress = userId.HasValue
            ? await _progressService.GetTopicProgressPercentsAsync(userId.Value, topic.Language.Title, cancellationToken)
            : new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        return new TheoryPageResponse
        {
            LanguageId = topic.Language.Title,
            TopicId = resolvedTopicId,
            TopicTitle = topic.Title,
            Difficulty = topic.Difficulty,
            Description = topic.Description,
            EstimatedMinutes = topic.EstimatedMinutes,
            PageIndex = page,
            PageCount = pages.Count,
            IsRead = isRead,
            TopicProgressPercent = topicProgress.GetValueOrDefault(resolvedTopicId),
            Markdown = pages[page - 1],
        };
    }

    public async Task<IReadOnlyList<int>> GetReadPageIndicesAsync(
        int userId, string languageId, string topicId, CancellationToken cancellationToken)
    {
        return await _progressService.GetReadPageIndicesAsync(userId, languageId, topicId, cancellationToken);
    }

    public async Task<TheoryProgressUpdateResponse?> MarkPageReadAsync(
        int userId,
        string languageId,
        string topicId,
        int page,
        CancellationToken cancellationToken)
    {
        await SeedDefaultTheoryAsync(cancellationToken);
        return await _progressService.MarkPageReadAsync(userId, languageId, topicId, page, cancellationToken);
    }

    private async Task<string?> ReadMarkdownObjectAsync(string markdownObjectName, CancellationToken cancellationToken)
    {
        var objectName = BuildObjectName(markdownObjectName);
        if (objectName is null)
        {
            return null;
        }

        using var memoryStream = new MemoryStream();
        var getObjectArgs = new GetObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(objectName)
            .WithCallbackStream(stream => stream.CopyTo(memoryStream));

        try
        {
            await _minioClient.GetObjectAsync(getObjectArgs, cancellationToken);
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
        catch
        {
            return null;
        }
    }

    private async Task PutMarkdownObjectAsync(string markdownObjectName, string content, CancellationToken cancellationToken)
    {
        var objectName = BuildObjectName(markdownObjectName);
        if (objectName is null)
        {
            return;
        }

        await EnsureBucketAsync(cancellationToken);

        var bytes = Encoding.UTF8.GetBytes(content);
        using var stream = new MemoryStream(bytes);
        var putObjectArgs = new PutObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType("text/markdown");

        await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);
    }

    private async Task RemoveMarkdownObjectAsync(string markdownObjectName, CancellationToken cancellationToken)
    {
        var objectName = BuildObjectName(markdownObjectName);
        if (objectName is null)
        {
            return;
        }

        try
        {
            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(objectName);

            await _minioClient.RemoveObjectAsync(removeObjectArgs, cancellationToken);
        }
        catch (ObjectNotFoundException)
        {
            // Deleting content should stay idempotent when the object is already gone.
        }
    }

    private async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        if (!_options.AutoCreateBucket)
        {
            return;
        }

        var exists = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(_options.BucketName), cancellationToken);
        if (!exists)
        {
            await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(_options.BucketName), cancellationToken);
        }
    }

    private async Task<IReadOnlyList<string>> ReadTopicPagesAsync(TheoryContent content, CancellationToken cancellationToken)
    {
        if (!UsesPagedMarkdownObjects(content.MarkdownObjectName))
        {
            var markdown = await ReadMarkdownObjectAsync(content.MarkdownObjectName, cancellationToken);
            return string.IsNullOrWhiteSpace(markdown) ? [] : SplitPages(markdown);
        }

        var pages = new List<string>();
        for (var pageIndex = 1; pageIndex <= content.PageCount; pageIndex += 1)
        {
            var markdown = await ReadMarkdownObjectAsync(BuildPageObjectName(content.MarkdownObjectName, pageIndex), cancellationToken);
            if (string.IsNullOrWhiteSpace(markdown))
            {
                continue;
            }

            pages.Add(markdown.Trim());
        }

        return pages;
    }

    private async Task WriteTopicPagesAsync(string markdownObjectName, IReadOnlyList<string> pages, CancellationToken cancellationToken)
    {
        if (!UsesPagedMarkdownObjects(markdownObjectName))
        {
            await PutMarkdownObjectAsync(markdownObjectName, string.Join(PageSeparator, pages), cancellationToken);
            return;
        }

        for (var pageIndex = 1; pageIndex <= pages.Count; pageIndex += 1)
        {
            await PutMarkdownObjectAsync(
                BuildPageObjectName(markdownObjectName, pageIndex),
                pages[pageIndex - 1],
                cancellationToken);
        }
    }

    private async Task<IReadOnlyList<string>> ReadSeedPagesAsync(TheoryTopicSeed topicSeed, CancellationToken cancellationToken)
    {
        var topicDirectory = Path.Combine(
            _environment.ContentRootPath,
            "TheoryContent",
            topicSeed.MarkdownObjectName.Replace('/', Path.DirectorySeparatorChar));

        if (!Directory.Exists(topicDirectory))
        {
            throw new DirectoryNotFoundException($"Theory seed directory was not found: {topicDirectory}");
        }

        var pageFiles = Directory
            .EnumerateFiles(topicDirectory, "*.md", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (pageFiles.Count == 0)
        {
            throw new InvalidOperationException($"Theory seed topic has no Markdown pages: {topicSeed.MarkdownObjectName}");
        }

        var pages = new List<string>(pageFiles.Count);
        foreach (var pageFile in pageFiles)
        {
            pages.Add((await File.ReadAllTextAsync(pageFile, Encoding.UTF8, cancellationToken)).Trim());
        }

        return pages;
    }

    private async Task<bool> ShouldResetSeededTopicsAsync(CancellationToken cancellationToken)
    {
        var manifest = await ReadMarkdownObjectAsync(_options.ManifestObject, cancellationToken);
        return manifest?.Contains(SeedVersion, StringComparison.Ordinal) != true;
    }

    private async Task WriteSeedManifestAsync(CancellationToken cancellationToken)
    {
        await PutMarkdownObjectAsync(
            _options.ManifestObject,
            $$"""
            {
              "seedVersion": "{{SeedVersion}}"
            }
            """,
            cancellationToken);
    }

    private async Task ResetTheoryTopicsAsync(IReadOnlyCollection<TheoryLanguageSeed> languageSeeds, CancellationToken cancellationToken)
    {
        await _dbContext.TheoryPageReadProgresses.ExecuteDeleteAsync(cancellationToken);
        await _dbContext.TheoryContents.ExecuteDeleteAsync(cancellationToken);
        await _dbContext.TheoryTopics.ExecuteDeleteAsync(cancellationToken);

        var seedLanguageCodes = languageSeeds
            .Select(language => language.Title)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Hard-delete any languages that fall outside the seeded set.
        var staleLanguages = await _dbContext.TheoryLanguages
            .Where(language => !seedLanguageCodes.Contains(language.Title))
            .ToListAsync(cancellationToken);

        if (staleLanguages.Count > 0)
        {
            _dbContext.TheoryLanguages.RemoveRange(staleLanguages);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _progressService.InvalidateAllProgressCacheAsync(cancellationToken);
    }

    private async Task SeedDefaultTheoryAsync(CancellationToken cancellationToken)
    {
        if (!_options.SeedDefaults)
        {
            return;
        }

        var languageSeeds = GetDefaultTheorySeeds().ToList();
        var resetTopics = await ShouldResetSeededTopicsAsync(cancellationToken);
        if (resetTopics)
        {
            await ResetTheoryTopicsAsync(languageSeeds, cancellationToken);
        }

        foreach (var languageSeed in languageSeeds)
        {
            var language = await _dbContext.TheoryLanguages
                .Include(item => item.Topics)
                    .ThenInclude(topic => topic.Content)
                .FirstOrDefaultAsync(item => item.Title == languageSeed.Title, cancellationToken);

            if (language is null)
            {
                language = new TheoryLanguage
                {
                    Title = languageSeed.Title,
                    Description = languageSeed.Description,
                    SortOrder = languageSeed.SortOrder,
                };

                _dbContext.TheoryLanguages.Add(language);
            }

            language.Title = languageSeed.Title;
            language.Description = languageSeed.Description;
            language.SortOrder = languageSeed.SortOrder;

            foreach (var topicSeed in languageSeed.Topics)
            {
                var existingTopic = language.Topics.FirstOrDefault(topic =>
                    string.Equals(topic.Title, topicSeed.Title, StringComparison.OrdinalIgnoreCase));
                if (existingTopic is not null)
                {
                    existingTopic.Title = topicSeed.Title;
                    existingTopic.Difficulty = topicSeed.Difficulty;
                    existingTopic.Description = topicSeed.Description;
                    existingTopic.EstimatedMinutes = topicSeed.EstimatedMinutes;
                    existingTopic.SortOrder = topicSeed.SortOrder;

                    if (existingTopic.Content is null)
                    {
                        var restoredPages = await ReadSeedPagesAsync(topicSeed, cancellationToken);
                        await WriteTopicPagesAsync(topicSeed.MarkdownObjectName, restoredPages, cancellationToken);

                        existingTopic.Content = new TheoryContent
                        {
                            MarkdownObjectName = topicSeed.MarkdownObjectName,
                            PageCount = restoredPages.Count,
                        };
                    }
                    else
                    {
                        existingTopic.Content.MarkdownObjectName = topicSeed.MarkdownObjectName;
                    }

                    continue;
                }

                var seedPages = await ReadSeedPagesAsync(topicSeed, cancellationToken);
                await WriteTopicPagesAsync(topicSeed.MarkdownObjectName, seedPages, cancellationToken);

                language.Topics.Add(new TheoryTopic
                {
                    Title = topicSeed.Title,
                    Difficulty = topicSeed.Difficulty,
                    Description = topicSeed.Description,
                    EstimatedMinutes = topicSeed.EstimatedMinutes,
                    SortOrder = topicSeed.SortOrder,
                    Content = new TheoryContent
                    {
                        MarkdownObjectName = topicSeed.MarkdownObjectName,
                        PageCount = seedPages.Count,
                    },
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await WriteSeedManifestAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error)> DeleteTopicAsync(
        string languageCode,
        string topicSlug,
        CancellationToken cancellationToken)
    {
        await SeedDefaultTheoryAsync(cancellationToken);

        var normalizedLanguageCode = languageCode.Trim().ToLowerInvariant();

        var topic = await FindTopicWithContentAsync(normalizedLanguageCode, topicSlug, asNoTracking: false, cancellationToken);

        if (topic is null)
        {
            return (false, "Tēma nav atrasta.");
        }

        if (topic.Content is not null)
        {
            await RemoveTopicObjectsAsync(topic.Content, cancellationToken);
            await _dbContext.TheoryPageReadProgresses
                .Where(progress => progress.TheoryContentId == topic.Content.Id)
                .ExecuteDeleteAsync(cancellationToken);
        }

        _dbContext.TheoryTopics.Remove(topic);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _progressService.InvalidateAllProgressCacheAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteContentPageAsync(
        string languageCode,
        string topicSlug,
        int page,
        CancellationToken cancellationToken)
    {
        await SeedDefaultTheoryAsync(cancellationToken);

        var normalizedLanguageCode = languageCode.Trim().ToLowerInvariant();

        var topic = await FindTopicWithContentAsync(normalizedLanguageCode, topicSlug, asNoTracking: false, cancellationToken);

        if (topic?.Content is null)
        {
            return (false, "Tēma vai tās saturs nav atrasts.");
        }

        var pages = (await ReadTopicPagesAsync(topic.Content, cancellationToken)).ToList();
        if (page < 1 || page > pages.Count)
        {
            return (false, "Teorijas lapa nav atrasta.");
        }

        if (pages.Count <= 1)
        {
            return (false, "Nevar dzēst pēdējo teorijas lapu. Dzēs visu tēmu, ja saturs vairs nav vajadzīgs.");
        }

        pages.RemoveAt(page - 1);
        await WriteTopicPagesAsync(topic.Content.MarkdownObjectName, pages, cancellationToken);

        if (UsesPagedMarkdownObjects(topic.Content.MarkdownObjectName))
        {
            await RemoveMarkdownObjectAsync(
                BuildPageObjectName(topic.Content.MarkdownObjectName, pages.Count + 1),
                cancellationToken);
        }

        topic.Content.PageCount = pages.Count;
        topic.Content.Version += 1;

        await _dbContext.TheoryPageReadProgresses
            .Where(progress => progress.TheoryContentId == topic.Content.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _progressService.InvalidateAllProgressCacheAsync(cancellationToken);
        return (true, null);
    }

    private async Task RemoveTopicObjectsAsync(TheoryContent content, CancellationToken cancellationToken)
    {
        if (!UsesPagedMarkdownObjects(content.MarkdownObjectName))
        {
            await RemoveMarkdownObjectAsync(content.MarkdownObjectName, cancellationToken);
            return;
        }

        for (var pageIndex = 1; pageIndex <= content.PageCount; pageIndex += 1)
        {
            await RemoveMarkdownObjectAsync(
                BuildPageObjectName(content.MarkdownObjectName, pageIndex),
                cancellationToken);
        }
    }

    public async Task<(bool Success, string? Error)> ApplyTopicRequestAsync(TheoryTopicRequest request, CancellationToken cancellationToken)
    {
        if (request.RequestType == TheoryTopicRequestType.Edit)
        {
            var topic = await FindTopicAsync(request.LanguageCode, request.TopicSlug, cancellationToken);

            if (topic is null)
                return (false, "Tēma nav atrasta.");

            if (request.ProposedTitle is not null) topic.Title = request.ProposedTitle;
            if (request.ProposedDescription is not null) topic.Description = request.ProposedDescription;
            if (request.ProposedDifficulty is not null) topic.Difficulty = request.ProposedDifficulty;
            if (request.ProposedEstimatedMinutes is not null) topic.EstimatedMinutes = request.ProposedEstimatedMinutes.Value;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return (true, null);
        }

        if (request.RequestType == TheoryTopicRequestType.Add)
        {
            var language = await _dbContext.TheoryLanguages
                .Include(l => l.Topics)
                .FirstOrDefaultAsync(l => l.Title.ToLower() == request.LanguageCode, cancellationToken);

            if (language is null)
                return (false, "Programmēšanas valoda nav atrasta.");

            if (language.Topics.Any(t => TheoryTopicKey.Matches(t.Title, request.TopicSlug)))
                return (false, "Tēma ar šādu identifikatoru jau pastāv.");

            var topicKey = TheoryTopicKey.FromTitle(request.ProposedTitle!);
            var objectName = $"{request.LanguageCode}/{topicKey}";
            var markdown = request.ProposedMarkdown ?? string.Empty;
            var pages = SplitPages(markdown);
            await WriteTopicPagesAsync(objectName, pages, cancellationToken);

            var maxSortOrder = language.Topics.Count > 0 ? language.Topics.Max(t => t.SortOrder) : 0;

            language.Topics.Add(new TheoryTopic
            {
                Title = request.ProposedTitle!,
                Difficulty = request.ProposedDifficulty!,
                Description = request.ProposedDescription!,
                EstimatedMinutes = request.ProposedEstimatedMinutes!.Value,
                SortOrder = maxSortOrder + 1,
                Content = new TheoryContent
                {
                    MarkdownObjectName = objectName,
                    PageCount = pages.Count,
                },
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            await _progressService.InvalidateAllProgressCacheAsync(cancellationToken);
            return (true, null);
        }

        return (false, "Nezināms pieprasījuma tips.");
    }

    public async Task<(bool Success, string? Error)> ApplyContentRequestAsync(TheoryContentRequest request, CancellationToken cancellationToken)
    {
        var topic = await FindTopicWithContentAsync(request.LanguageCode, request.TopicSlug, asNoTracking: false, cancellationToken);

        if (topic?.Content is null)
            return (false, "Tēma vai tās saturs nav atrasts.");

        var pages = (await ReadTopicPagesAsync(topic.Content, cancellationToken)).ToList();

        if (request.RequestType == TheoryContentRequestType.Edit)
        {
            if (request.PageIndex is null || request.PageIndex < 1 || request.PageIndex > pages.Count)
            {
                return (false, "Rediģējamā teorijas lapa nav atrasta.");
            }

            pages[request.PageIndex.Value - 1] = request.ProposedMarkdown.Trim();
        }
        else
        {
            pages.Add(request.ProposedMarkdown.Trim());
        }

        await WriteTopicPagesAsync(topic.Content.MarkdownObjectName, pages, cancellationToken);

        topic.Content.PageCount = pages.Count;
        topic.Content.Version += 1;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _progressService.InvalidateAllProgressCacheAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ApplyQuizRequestAsync(
        TheoryQuizRequest request,
        CancellationToken cancellationToken)
    {
        var questions = TheoryRequestsController.ReadQuizQuestions(request.ProposedQuestionsJson);
        if (questions.Count == 0)
        {
            return (false, "Testam nav jautājumu.");
        }

        var topic = await FindTopicWithQuizAsync(request.LanguageCode, request.TopicSlug, cancellationToken);

        if (topic is null)
        {
            return (false, "Tēma nav atrasta.");
        }

        if (request.RequestType == TheoryQuizRequestType.Add && topic.Quiz is not null)
        {
            return (false, "Šai tēmai tests jau eksistē.");
        }

        if (request.RequestType == TheoryQuizRequestType.Edit && topic.Quiz is null)
        {
            return (false, "Šai tēmai vēl nav testa, ko rediģēt.");
        }

        var quiz = topic.Quiz;
        if (quiz is null)
        {
            quiz = new TheoryQuiz
            {
                TopicId = topic.Id,
            };
            _dbContext.TheoryQuizzes.Add(quiz);
        }
        else
        {
            _dbContext.TheoryQuizQuestions.RemoveRange(quiz.Questions);
            quiz.Questions.Clear();
        }

        quiz.Title = request.ProposedTitle.Trim();
        quiz.Description = request.ProposedDescription.Trim();

        foreach (var question in questions.Select((value, index) => new { value, index }))
        {
            quiz.Questions.Add(new TheoryQuizQuestion
            {
                OrderIndex = question.index,
                Prompt = question.value.Prompt.Trim(),
                Explanation = string.IsNullOrWhiteSpace(question.value.Explanation) ? null : question.value.Explanation.Trim(),
                Options = question.value.Options
                    .Select((option, optionIndex) => new TheoryQuizOption
                    {
                        OrderIndex = optionIndex,
                        Text = option.Trim(),
                        IsCorrect = optionIndex == question.value.CorrectOptionIndex,
                    })
                    .ToList(),
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    private async Task<TheoryTopic?> FindTopicAsync(
        string languageCode,
        string topicId,
        CancellationToken cancellationToken)
    {
        var normalizedLanguageCode = languageCode.Trim().ToLowerInvariant();
        var normalizedTopicId = TheoryTopicKey.Normalize(topicId);

        var topics = await _dbContext.TheoryTopics
            .Include(topic => topic.Language)
            .Where(topic => topic.Language != null && topic.Language.Title.ToLower() == normalizedLanguageCode)
            .ToListAsync(cancellationToken);

        return topics.FirstOrDefault(topic => TheoryTopicKey.Matches(topic.Title, normalizedTopicId));
    }

    private async Task<TheoryTopic?> FindTopicWithContentAsync(
        string languageCode,
        string topicId,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var normalizedLanguageCode = languageCode.Trim().ToLowerInvariant();
        var normalizedTopicId = TheoryTopicKey.Normalize(topicId);

        IQueryable<TheoryTopic> query = _dbContext.TheoryTopics;
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        var topics = await query
            .Include(topic => topic.Language)
            .Include(topic => topic.Content)
            .Where(topic => topic.Language != null && topic.Language.Title.ToLower() == normalizedLanguageCode)
            .ToListAsync(cancellationToken);

        return topics.FirstOrDefault(topic => TheoryTopicKey.Matches(topic.Title, normalizedTopicId));
    }

    private async Task<TheoryTopic?> FindTopicWithQuizAsync(
        string languageCode,
        string topicId,
        CancellationToken cancellationToken)
    {
        var normalizedLanguageCode = languageCode.Trim().ToLowerInvariant();
        var normalizedTopicId = TheoryTopicKey.Normalize(topicId);

        var topics = await _dbContext.TheoryTopics
            .Include(topic => topic.Language)
            .Include(topic => topic.Quiz)
                .ThenInclude(quiz => quiz!.Questions)
                    .ThenInclude(question => question.Options)
            .Where(topic => topic.Language != null && topic.Language.Title.ToLower() == normalizedLanguageCode)
            .ToListAsync(cancellationToken);

        return topics.FirstOrDefault(topic => TheoryTopicKey.Matches(topic.Title, normalizedTopicId));
    }

    private async Task UpdatePageCountAsync(int contentId, int pageCount, CancellationToken cancellationToken)
    {
        await _dbContext.TheoryContents
            .Where(content => content.Id == contentId)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(content => content.PageCount, pageCount),
                cancellationToken);
        await _progressService.InvalidateAllProgressCacheAsync(cancellationToken);
    }

    private string? BuildObjectName(string objectName)
    {
        var cleanedObjectName = objectName.Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(cleanedObjectName) || cleanedObjectName.Contains("..", StringComparison.Ordinal))
        {
            return null;
        }

        var prefix = _options.Prefix.Trim('/');
        if (string.IsNullOrWhiteSpace(prefix) || cleanedObjectName.StartsWith($"{prefix}/", StringComparison.OrdinalIgnoreCase))
        {
            return cleanedObjectName;
        }

        return $"{prefix}/{cleanedObjectName}";
    }

    private static string BuildPageObjectName(string objectPrefix, int pageIndex)
    {
        return $"{objectPrefix.Replace('\\', '/').TrimEnd('/')}/page-{pageIndex:00}.md";
    }

    private static bool UsesPagedMarkdownObjects(string markdownObjectName)
    {
        return !markdownObjectName.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetLanguageImageUrl(string languageCode)
    {
        return $"/theory/{languageCode.ToLowerInvariant()}.png";
    }

    private static List<string> SplitPages(string markdown)
    {
        return markdown
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split(PageSeparator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }

    private static IEnumerable<TheoryLanguageSeed> GetDefaultTheorySeeds()
    {
        return
        [
            new TheoryLanguageSeed(
                Title: "Python",
                Description: "Lasāma, praktiska valoda pirmajiem soļiem, automatizācijai, datu apstrādei un tīmekļa lietotnēm.",
                SortOrder: 1,
                Topics:
                [
                    new TheoryTopicSeed(
                        Title: "Ievads un darba vide",
                        Difficulty: "Iesācējs",
                        Description: "Python loma, interpretators, failu palaišana, REPL un pirmā programma.",
                        EstimatedMinutes: 30,
                        SortOrder: 1,
                        MarkdownObjectName: "python/ievads-un-vide"),
                    new TheoryTopicSeed(
                        Title: "Mainīgie un datu tipi",
                        Difficulty: "Iesācējs",
                        Description: "Vērtību saglabāšana, skaitļi, teksts, booleans, None un tipu pārveidošana.",
                        EstimatedMinutes: 35,
                        SortOrder: 2,
                        MarkdownObjectName: "python/mainigie-un-tipi"),
                    new TheoryTopicSeed(
                        Title: "Nosacījumi un loģika",
                        Difficulty: "Iesācējs",
                        Description: "if, elif, else, salīdzinājumi, loģiskie operatori un patiesuma vērtības.",
                        EstimatedMinutes: 30,
                        SortOrder: 3,
                        MarkdownObjectName: "python/nosacijumi-un-logika"),
                    new TheoryTopicSeed(
                        Title: "Cikli",
                        Difficulty: "Iesācējs",
                        Description: "for, while, range, iterēšana, break, continue un droši ciklu paradumi.",
                        EstimatedMinutes: 35,
                        SortOrder: 4,
                        MarkdownObjectName: "python/cikli"),
                ]),
            new TheoryLanguageSeed(
                Title: "Java",
                Description: "Stipri tipizēta, objektorientēta valoda lielām lietotnēm un Android.",
                SortOrder: 2,
                Topics:
                [
                    new TheoryTopicSeed(
                        Title: "Java ievads un darba vide",
                        Difficulty: "Iesācējs",
                        Description: "JVM, .java pirmkods, kompilācija ar javac un palaišana ar java.",
                        EstimatedMinutes: 25,
                        SortOrder: 1,
                        MarkdownObjectName: "java/ievads-un-vide"),
                ]),
        ];
    }

    private sealed record TheoryLanguageSeed(
        string Title,
        string Description,
        int SortOrder,
        IReadOnlyList<TheoryTopicSeed> Topics);

    private sealed record TheoryTopicSeed(
        string Title,
        string Difficulty,
        string Description,
        int EstimatedMinutes,
        int SortOrder,
        string MarkdownObjectName);
}
