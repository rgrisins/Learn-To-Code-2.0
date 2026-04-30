using LearnToCode.API.Services;
using LearnToCode.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Minio;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
}

var redisConnection = builder.Configuration["Redis:Connection"];

if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    {
        var options = ConfigurationOptions.Parse(redisConnection);
        options.AbortOnConnectFail = false;
        return ConnectionMultiplexer.Connect(options);
    });
}

builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthSessionService, RedisAuthSessionService>();
builder.Services.Configure<TheoryStorageOptions>(builder.Configuration.GetSection("Theory:Storage"));
builder.Services.AddScoped<TheoryProgressService>();
builder.Services.AddScoped<TheoryCatalogService>();
builder.Services.AddScoped<DockerCodeRunnerService>();

builder.Services.AddMinio(configureClient =>
{
    var endpoint = builder.Configuration["Minio:Endpoint"] ?? "localhost:9000";
    var accessKey = builder.Configuration["Minio:AccessKey"] ?? "minioadmin";
    var secretKey = builder.Configuration["Minio:SecretKey"] ?? "minioadmin";
    var useSsl = builder.Configuration.GetValue("Minio:UseSsl", false);

    configureClient
        .WithEndpoint(endpoint)
        .WithCredentials(accessKey, secretKey)
        .WithSSL(useSsl)
        .Build();
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtIssuer = builder.Configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        var jwtAudience = builder.Configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience is not configured.");
        var jwtSecret = builder.Configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

        options.MapInboundClaims = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (!string.IsNullOrWhiteSpace(context.Token))
                {
                    return Task.CompletedTask;
                }

                if (!context.Request.Cookies.TryGetValue(AuthCookieDefaults.RefreshTokenCookieName, out var refreshToken) ||
                    string.IsNullOrWhiteSpace(refreshToken))
                {
                    return Task.CompletedTask;
                }

                var sessionService = context.HttpContext.RequestServices.GetRequiredService<IAuthSessionService>();
                var tokenService = context.HttpContext.RequestServices.GetRequiredService<ITokenService>();
                var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

                return ResolveAccessTokenAsync(context, sessionService, tokenService, dbContext, refreshToken);
            },
            OnTokenValidated = async context =>
            {
                var sessionService = context.HttpContext.RequestServices.GetRequiredService<IAuthSessionService>();
                var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

                var sessionId = FindClaimValue(context, "sid");
                var jwtId = FindClaimValue(context, JwtRegisteredClaimNames.Jti);
                var userIdClaim = FindClaimValue(context, ClaimTypes.NameIdentifier, JwtRegisteredClaimNames.Sub, "sub", "nameid");
                var rawToken = GetRawToken(context);

                if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(jwtId) || string.IsNullOrWhiteSpace(rawToken) || string.IsNullOrWhiteSpace(userIdClaim))
                {
                    context.Fail($"Missing token claims. sid={sessionId}, jti={jwtId}, uid={userIdClaim}");
                    return;
                }

                // Try to validate existing session
                var isValid = await sessionService.ValidateSessionAsync(sessionId, jwtId, rawToken, context.HttpContext.RequestAborted);
                if (isValid)
                {
                    return;
                }

                // Session validation failed - try to restore it if JWT is still valid
                if (!int.TryParse(userIdClaim, out var userId))
                {
                    context.Fail($"Invalid user ID in token: {userIdClaim}");
                    return;
                }

                var user = await dbContext.Users.FindAsync(new object[] { userId }, cancellationToken: context.HttpContext.RequestAborted);
                if (user is null)
                {
                    context.Fail($"User not found. UserId={userId}");
                    return;
                }

                if (context.SecurityToken.ValidTo <= DateTime.UtcNow)
                {
                    context.Fail("Token has expired.");
                    return;
                }

                var tokenResult = new TokenGenerationResult
                {
                    Token = rawToken,
                    JwtId = jwtId,
                    ExpiresAtUtc = context.SecurityToken.ValidTo,
                };

                await sessionService.CreateSessionAsync(sessionId, user, tokenResult, rawToken, context.HttpContext.RequestAborted);
            }
        };
    }); ;

builder.Services.AddAuthorization();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? error.Exception?.Message ?? "Invalid value." : error.ErrorMessage).ToArray());

        return new BadRequestObjectResult(new
        {
            message = "Request validation failed.",
            errors,
        });
    };
});

builder.Services.AddOpenApi();

// Configure ForwardedHeaders middleware to handle proxy headers
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

await EnsureApplicationTablesAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => Results.Text("LearnToCode API is running."));
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

static async Task ResolveAccessTokenAsync(MessageReceivedContext context, IAuthSessionService sessionService, ITokenService tokenService, AppDbContext dbContext, string refreshToken)
{
    var session = await sessionService.GetSessionAsync(refreshToken, context.HttpContext.RequestAborted);
    if (session is null)
    {
        context.HttpContext.Response.Cookies.Delete(AuthCookieDefaults.RefreshTokenCookieName, AuthCookieDefaults.CreateDeletionCookieOptions(context.Request));
        return;
    }

    if (session.ExpiresAtUtc <= DateTime.UtcNow)
    {
        await sessionService.DeleteSessionAsync(refreshToken, context.HttpContext.RequestAborted);
        context.HttpContext.Response.Cookies.Delete(AuthCookieDefaults.RefreshTokenCookieName, AuthCookieDefaults.CreateDeletionCookieOptions(context.Request));
        return;
    }

    if (session.AccessTokenExpiresAtUtc <= DateTime.UtcNow || string.IsNullOrWhiteSpace(session.AccessToken))
    {
        var user = await dbContext.Users.FindAsync(new object[] { session.UserId }, cancellationToken: context.HttpContext.RequestAborted);
        if (user is null)
        {
            await sessionService.DeleteSessionAsync(refreshToken, context.HttpContext.RequestAborted);
            context.HttpContext.Response.Cookies.Delete(AuthCookieDefaults.RefreshTokenCookieName, AuthCookieDefaults.CreateDeletionCookieOptions(context.Request));
            return;
        }

        var tokenResult = tokenService.CreateToken(user, session.SessionId);
        session = await sessionService.UpdateAccessTokenAsync(session.SessionId, tokenResult, context.HttpContext.RequestAborted)
            ?? await sessionService.CreateSessionAsync(session.SessionId, user, tokenResult, tokenResult.Token, context.HttpContext.RequestAborted);
    }

    context.Token = session.AccessToken;
}

static string? FindClaimValue(TokenValidatedContext context, params string[] claimTypes)
{
    foreach (var claimType in claimTypes)
    {
        var value = context.Principal?.FindFirstValue(claimType);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }
    }

    foreach (var claimType in claimTypes)
    {
        var value = context.SecurityToken switch
        {
            JwtSecurityToken jwtToken => jwtToken.Claims.FirstOrDefault(claim => claim.Type == claimType)?.Value,
            Microsoft.IdentityModel.JsonWebTokens.JsonWebToken jsonWebToken => jsonWebToken.Claims.FirstOrDefault(claim => claim.Type == claimType)?.Value,
            _ => null,
        };

        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }
    }

    return null;
}

static string? GetRawToken(TokenValidatedContext context)
{
    return context.SecurityToken switch
    {
        JwtSecurityToken jwtToken => jwtToken.RawData,
        Microsoft.IdentityModel.JsonWebTokens.JsonWebToken jsonWebToken => jsonWebToken.EncodedToken,
        _ => TryReadBearerToken(context.HttpContext.Request),
    };
}

static string? TryReadBearerToken(HttpRequest request)
{
    var authorization = request.Headers.Authorization.ToString();
    const string bearerPrefix = "Bearer ";

    return authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)
        ? authorization[bearerPrefix.Length..].Trim()
        : null;
}

static async Task EnsureApplicationTablesAsync(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetService<AppDbContext>();
    if (dbContext is null)
    {
        return;
    }

    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "Users" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "Username" character varying(100) NULL,
            "NormalizedUsername" character varying(100) NULL,
            "FirstName" character varying(100) NULL,
            "LastName" character varying(100) NULL,
            "BirthDate" date NULL,
            "FullName" character varying(200) NOT NULL,
            "Email" character varying(320) NOT NULL,
            "NormalizedEmail" character varying(320) NOT NULL,
            "PasswordHash" text NOT NULL,
            "EducationInstitution" character varying(200) NULL,
            "Rating" integer NOT NULL DEFAULT 1000,
            "Role" character varying(32) NOT NULL,
            "CreatedAtUtc" timestamp with time zone NOT NULL,
            "UpdatedAtUtc" timestamp with time zone NOT NULL
        );

        CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_NormalizedUsername" ON "Users" ("NormalizedUsername");
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_NormalizedEmail" ON "Users" ("NormalizedEmail");
        """);

    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "RoleRequests" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "UserId" integer NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
            "RequestedRole" character varying(32) NOT NULL,
            "Reason" character varying(1000) NOT NULL,
            "Status" character varying(32) NOT NULL,
            "CreatedAtUtc" timestamp with time zone NOT NULL
        );

        CREATE INDEX IF NOT EXISTS "IX_RoleRequests_UserId_Status" ON "RoleRequests" ("UserId", "Status");

        CREATE TABLE IF NOT EXISTS "TheoryLanguages" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "Title" character varying(120) NOT NULL,
            "Description" character varying(600) NOT NULL,
            "SortOrder" integer NOT NULL
        );

        CREATE UNIQUE INDEX IF NOT EXISTS "IX_TheoryLanguages_Title" ON "TheoryLanguages" ("Title");

        CREATE TABLE IF NOT EXISTS "TheoryTopics" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "LanguageId" integer NOT NULL REFERENCES "TheoryLanguages"("Id") ON DELETE CASCADE,
            "Title" character varying(160) NOT NULL,
            "Difficulty" character varying(40) NOT NULL,
            "Description" character varying(900) NOT NULL,
            "EstimatedMinutes" integer NOT NULL,
            "SortOrder" integer NOT NULL
        );

        CREATE TABLE IF NOT EXISTS "TheoryContents" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "TopicId" integer NOT NULL REFERENCES "TheoryTopics"("Id") ON DELETE CASCADE,
            "MarkdownObjectName" character varying(500) NOT NULL,
            "PageCount" integer NOT NULL DEFAULT 1,
            "Version" integer NOT NULL DEFAULT 1
        );

        CREATE UNIQUE INDEX IF NOT EXISTS "IX_TheoryContents_TopicId" ON "TheoryContents" ("TopicId");

        CREATE TABLE IF NOT EXISTS "TheoryPageReadProgresses" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "UserId" integer NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
            "TheoryContentId" integer NOT NULL REFERENCES "TheoryContents"("Id") ON DELETE CASCADE,
            "ContentVersion" integer NOT NULL,
            "PageIndex" integer NOT NULL,
            "ReadAtUtc" timestamp with time zone NOT NULL
        );

        CREATE INDEX IF NOT EXISTS "IX_TheoryPageReadProgresses_UserId" ON "TheoryPageReadProgresses" ("UserId");
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_TheoryPageReadProgresses_User_Content_Page" ON "TheoryPageReadProgresses" ("UserId", "TheoryContentId", "ContentVersion", "PageIndex");

        CREATE TABLE IF NOT EXISTS "TheoryTopicRequests" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "UserId" integer NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
            "RequestType" character varying(32) NOT NULL,
            "LanguageCode" character varying(64) NOT NULL,
            "TopicSlug" character varying(96) NOT NULL,
            "ProposedTitle" character varying(160) NULL,
            "ProposedDescription" character varying(900) NULL,
            "ProposedDifficulty" character varying(40) NULL,
            "ProposedEstimatedMinutes" integer NULL,
            "ProposedMarkdown" text NULL,
            "Status" character varying(32) NOT NULL,
            "CreatedAtUtc" timestamp with time zone NOT NULL
        );

        CREATE INDEX IF NOT EXISTS "IX_TheoryTopicRequests_UserId_Status" ON "TheoryTopicRequests" ("UserId", "Status");

        CREATE TABLE IF NOT EXISTS "TheoryContentRequests" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "UserId" integer NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
            "LanguageCode" character varying(64) NOT NULL,
            "TopicSlug" character varying(96) NOT NULL,
            "RequestType" character varying(32) NOT NULL DEFAULT 'Add',
            "PageIndex" integer NULL,
            "ProposedMarkdown" text NOT NULL,
            "Status" character varying(32) NOT NULL,
            "CreatedAtUtc" timestamp with time zone NOT NULL
        );

        CREATE INDEX IF NOT EXISTS "IX_TheoryContentRequests_UserId_Status" ON "TheoryContentRequests" ("UserId", "Status");

        CREATE TABLE IF NOT EXISTS "TheoryQuizRequests" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "UserId" integer NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
            "RequestType" character varying(32) NOT NULL,
            "LanguageCode" character varying(64) NOT NULL,
            "TopicSlug" character varying(96) NOT NULL,
            "ProposedTitle" character varying(160) NOT NULL,
            "ProposedDescription" character varying(900) NOT NULL,
            "ProposedQuestionsJson" text NOT NULL,
            "Status" character varying(32) NOT NULL,
            "CreatedAtUtc" timestamp with time zone NOT NULL
        );

        CREATE INDEX IF NOT EXISTS "IX_TheoryQuizRequests_UserId_Status" ON "TheoryQuizRequests" ("UserId", "Status");
        """);

    await dbContext.Database.ExecuteSqlRawAsync("""
        ALTER TABLE IF EXISTS "TheoryContentRequests"
        ADD COLUMN IF NOT EXISTS "RequestType" character varying(32) NOT NULL DEFAULT 'Add';

        ALTER TABLE IF EXISTS "TheoryContentRequests"
        ADD COLUMN IF NOT EXISTS "PageIndex" integer NULL;
        """);

    await dbContext.Database.ExecuteSqlRawAsync("""
        ALTER TABLE IF EXISTS "Exercises"
        ADD COLUMN IF NOT EXISTS "LanguageVersion" character varying(32) NOT NULL DEFAULT '3.11';
        """);

    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "Exercises" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "Title" character varying(200) NOT NULL,
            "Description" text NOT NULL DEFAULT '',
            "LanguageCode" character varying(64) NOT NULL DEFAULT 'python',
            "LanguageVersion" character varying(32) NOT NULL DEFAULT '3.11',
            "Difficulty" character varying(40) NOT NULL DEFAULT '',
            "SortOrder" integer NOT NULL DEFAULT 0,
            "AuthorId" integer NULL REFERENCES "Users"("Id") ON DELETE SET NULL,
            "CreatedAtUtc" timestamp with time zone NOT NULL
        );

        CREATE INDEX IF NOT EXISTS "IX_Exercises_SortOrder" ON "Exercises" ("SortOrder");

        CREATE TABLE IF NOT EXISTS "ExerciseRequests" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "AuthorId" integer NULL REFERENCES "Users"("Id") ON DELETE SET NULL,
            "Title" character varying(200) NOT NULL,
            "Description" text NOT NULL DEFAULT '',
            "Difficulty" character varying(40) NOT NULL DEFAULT '',
            "LanguageCode" character varying(64) NOT NULL,
            "LanguageVersion" character varying(32) NOT NULL DEFAULT '3.11',
            "SolutionLanguageCode" character varying(64) NOT NULL DEFAULT 'python',
            "SolutionCode" text NOT NULL DEFAULT '',
            "TestCasesJson" text NOT NULL DEFAULT '',
            "Status" character varying(32) NOT NULL DEFAULT 'Pending',
            "CreatedAtUtc" timestamp with time zone NOT NULL
        );

        CREATE INDEX IF NOT EXISTS "IX_ExerciseRequests_Status" ON "ExerciseRequests" ("Status");

        CREATE TABLE IF NOT EXISTS "ExerciseTestCases" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "ExerciseId" integer NOT NULL REFERENCES "Exercises"("Id") ON DELETE CASCADE,
            "Input" text NOT NULL DEFAULT '',
            "ExpectedOutput" text NOT NULL DEFAULT '',
            "IsHidden" boolean NOT NULL DEFAULT false,
            "OrderIndex" integer NOT NULL DEFAULT 0
        );

        CREATE INDEX IF NOT EXISTS "IX_ExerciseTestCases_ExerciseId" ON "ExerciseTestCases" ("ExerciseId");

        CREATE TABLE IF NOT EXISTS "ExerciseSubmissions" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "ExerciseId" integer NOT NULL REFERENCES "Exercises"("Id") ON DELETE CASCADE,
            "UserId" integer NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
            "LanguageCode" character varying(64) NOT NULL,
            "Code" text NOT NULL,
            "Status" character varying(32) NOT NULL,
            "TestsPassed" integer NOT NULL DEFAULT 0,
            "TestsTotal" integer NOT NULL DEFAULT 0,
            "ErrorMessage" character varying(500) NULL,
            "TestResultsJson" text NULL,
            "SubmittedAtUtc" timestamp with time zone NOT NULL,
            "ExecutedAtUtc" timestamp with time zone NULL
        );

        CREATE INDEX IF NOT EXISTS "IX_ExerciseSubmissions_ExerciseId_UserId" ON "ExerciseSubmissions" ("ExerciseId", "UserId");
        CREATE INDEX IF NOT EXISTS "IX_ExerciseSubmissions_UserId" ON "ExerciseSubmissions" ("UserId");
        """);

    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "TheoryQuizzes" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "TopicId" integer NOT NULL REFERENCES "TheoryTopics"("Id") ON DELETE CASCADE,
            "Title" character varying(160) NOT NULL,
            "Description" character varying(900) NOT NULL DEFAULT ''
        );

        CREATE UNIQUE INDEX IF NOT EXISTS "IX_TheoryQuizzes_TopicId" ON "TheoryQuizzes" ("TopicId");

        CREATE TABLE IF NOT EXISTS "TheoryQuizQuestions" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "QuizId" integer NOT NULL REFERENCES "TheoryQuizzes"("Id") ON DELETE CASCADE,
            "OrderIndex" integer NOT NULL DEFAULT 0,
            "Prompt" character varying(1000) NOT NULL,
            "Explanation" character varying(2000) NULL
        );

        CREATE INDEX IF NOT EXISTS "IX_TheoryQuizQuestions_QuizId_OrderIndex" ON "TheoryQuizQuestions" ("QuizId", "OrderIndex");

        CREATE TABLE IF NOT EXISTS "TheoryQuizOptions" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "QuestionId" integer NOT NULL REFERENCES "TheoryQuizQuestions"("Id") ON DELETE CASCADE,
            "OrderIndex" integer NOT NULL DEFAULT 0,
            "Text" character varying(500) NOT NULL,
            "IsCorrect" boolean NOT NULL DEFAULT false
        );

        CREATE INDEX IF NOT EXISTS "IX_TheoryQuizOptions_QuestionId_OrderIndex" ON "TheoryQuizOptions" ("QuestionId", "OrderIndex");

        CREATE TABLE IF NOT EXISTS "TheoryQuizQuestionAnswers" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "QuestionId" integer NOT NULL REFERENCES "TheoryQuizQuestions"("Id") ON DELETE CASCADE,
            "UserId" integer NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
            "SelectedOptionId" integer NULL REFERENCES "TheoryQuizOptions"("Id") ON DELETE SET NULL,
            "IsCorrect" boolean NOT NULL DEFAULT false,
            "AnsweredAtUtc" timestamp with time zone NOT NULL
        );

        ALTER TABLE IF EXISTS "TheoryQuizQuestionAnswers"
        ADD COLUMN IF NOT EXISTS "SelectedOptionId" integer NULL REFERENCES "TheoryQuizOptions"("Id") ON DELETE SET NULL;

        CREATE UNIQUE INDEX IF NOT EXISTS "IX_TheoryQuizQuestionAnswers_User_Question" ON "TheoryQuizQuestionAnswers" ("UserId", "QuestionId");
        """);

    // Schema cleanup for databases created before the current lean theory model.

    await dbContext.Database.ExecuteSqlRawAsync("""
        DO $$
        BEGIN
            IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'TheoryContents' AND column_name = 'IsPublished') THEN
                EXECUTE 'DELETE FROM "TheoryContents" WHERE "IsPublished" = false';
            END IF;

            IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'TheoryTopics' AND column_name = 'IsPublished') THEN
                EXECUTE 'DELETE FROM "TheoryTopics" WHERE "IsPublished" = false';
            END IF;

            IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'TheoryLanguages' AND column_name = 'IsPublished') THEN
                EXECUTE 'DELETE FROM "TheoryLanguages" WHERE "IsPublished" = false';
            END IF;

            IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'TheoryQuizzes' AND column_name = 'IsPublished') THEN
                EXECUTE 'DELETE FROM "TheoryQuizzes" WHERE "IsPublished" = false';
            END IF;

            IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Exercises' AND column_name = 'IsPublished') THEN
                EXECUTE 'DELETE FROM "Exercises" WHERE "IsPublished" = false';
            END IF;
        END $$;

        ALTER TABLE IF EXISTS "TheoryLanguages"  DROP COLUMN IF EXISTS "IsPublished";
        ALTER TABLE IF EXISTS "TheoryLanguages"  DROP COLUMN IF EXISTS "UpdatedAtUtc";
        ALTER TABLE IF EXISTS "TheoryLanguages"  DROP COLUMN IF EXISTS "CreatedAtUtc";
        DROP INDEX IF EXISTS "IX_TheoryLanguages_Code";
        ALTER TABLE IF EXISTS "TheoryLanguages"  DROP COLUMN IF EXISTS "Code";

        -- Only Python and Java are currently supported. Hard-delete any leftover
        -- language rows (javascript/csharp/php/etc.) and any exercise / submission
        -- rows that don't belong to one of the two.
        DELETE FROM "TheoryLanguages"
            WHERE LOWER("Title") NOT IN ('python', 'java');
        DELETE FROM "Exercises"
            WHERE LOWER("LanguageCode") NOT IN ('python', 'java');
        DELETE FROM "ExerciseSubmissions"
            WHERE LOWER("LanguageCode") NOT IN ('python', 'java');
        DELETE FROM "ExerciseRequests"
            WHERE LOWER("LanguageCode") NOT IN ('python', 'java');
        DROP INDEX IF EXISTS "IX_TheoryTopics_LanguageId_Slug";
        ALTER TABLE IF EXISTS "TheoryTopics"     DROP COLUMN IF EXISTS "Slug";
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_TheoryTopics_LanguageId_Title" ON "TheoryTopics" ("LanguageId", "Title");
        ALTER TABLE IF EXISTS "TheoryTopics"     DROP COLUMN IF EXISTS "IsPublished";
        ALTER TABLE IF EXISTS "TheoryTopics"     DROP COLUMN IF EXISTS "UpdatedAtUtc";
        ALTER TABLE IF EXISTS "TheoryTopics"     DROP COLUMN IF EXISTS "CreatedAtUtc";
        ALTER TABLE IF EXISTS "TheoryContents"   DROP COLUMN IF EXISTS "IsPublished";
        ALTER TABLE IF EXISTS "TheoryContents"   DROP COLUMN IF EXISTS "CreatedAtUtc";
        ALTER TABLE IF EXISTS "TheoryContents"   DROP COLUMN IF EXISTS "UpdatedAtUtc";
        ALTER TABLE IF EXISTS "TheoryQuizzes"    DROP COLUMN IF EXISTS "IsPublished";
        ALTER TABLE IF EXISTS "TheoryQuizzes"    DROP COLUMN IF EXISTS "UpdatedAtUtc";
        ALTER TABLE IF EXISTS "TheoryQuizzes"    DROP COLUMN IF EXISTS "CreatedAtUtc";

        ALTER TABLE IF EXISTS "RoleRequests"            DROP COLUMN IF EXISTS "ReviewComment";
        ALTER TABLE IF EXISTS "RoleRequests"            DROP COLUMN IF EXISTS "ReviewedByUserId";
        ALTER TABLE IF EXISTS "RoleRequests"            DROP COLUMN IF EXISTS "ReviewedAtUtc";
        ALTER TABLE IF EXISTS "RoleRequests"            DROP COLUMN IF EXISTS "UpdatedAtUtc";

        ALTER TABLE IF EXISTS "TheoryTopicRequests"     DROP COLUMN IF EXISTS "ReviewComment";
        ALTER TABLE IF EXISTS "TheoryTopicRequests"     DROP COLUMN IF EXISTS "ReviewedByUserId";
        ALTER TABLE IF EXISTS "TheoryTopicRequests"     DROP COLUMN IF EXISTS "ReviewedAtUtc";
        ALTER TABLE IF EXISTS "TheoryTopicRequests"     DROP COLUMN IF EXISTS "UpdatedAtUtc";

        ALTER TABLE IF EXISTS "TheoryContentRequests"   DROP COLUMN IF EXISTS "ReviewComment";
        ALTER TABLE IF EXISTS "TheoryContentRequests"   DROP COLUMN IF EXISTS "ReviewedByUserId";
        ALTER TABLE IF EXISTS "TheoryContentRequests"   DROP COLUMN IF EXISTS "ReviewedAtUtc";
        ALTER TABLE IF EXISTS "TheoryContentRequests"   DROP COLUMN IF EXISTS "UpdatedAtUtc";

        ALTER TABLE IF EXISTS "TheoryQuizRequests"      DROP COLUMN IF EXISTS "ReviewComment";
        ALTER TABLE IF EXISTS "TheoryQuizRequests"      DROP COLUMN IF EXISTS "ReviewedByUserId";
        ALTER TABLE IF EXISTS "TheoryQuizRequests"      DROP COLUMN IF EXISTS "ReviewedAtUtc";
        ALTER TABLE IF EXISTS "TheoryQuizRequests"      DROP COLUMN IF EXISTS "UpdatedAtUtc";

        DROP INDEX IF EXISTS "IX_Exercises_IsPublished_SortOrder";
        ALTER TABLE IF EXISTS "Exercises" DROP COLUMN IF EXISTS "IsPublished";
        ALTER TABLE IF EXISTS "Exercises" DROP COLUMN IF EXISTS "UpdatedAtUtc";
        ALTER TABLE IF EXISTS "Exercises" DROP COLUMN IF EXISTS "SampleInput";
        ALTER TABLE IF EXISTS "Exercises" DROP COLUMN IF EXISTS "SampleOutput";
        ALTER TABLE IF EXISTS "Exercises" DROP COLUMN IF EXISTS "SampleInputAlt";
        ALTER TABLE IF EXISTS "Exercises" DROP COLUMN IF EXISTS "SampleOutputAlt";
        """);

}
