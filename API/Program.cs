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

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(
                "https://app.learn-to-code.lat",
                "http://192.168.50.242:5173",
                "http://localhost:5173"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseRouting();

app.UseCors("Frontend");

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
