// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.RateLimiting;
using AssignmentSystem.Api.Configuration;
using AssignmentSystem.Api.Controllers;
using AssignmentSystem.Api.Filters;
using AssignmentSystem.Api.Middleware;
using AssignmentSystem.Api.Services;
using AssignmentSystem.Application;
using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Infrastructure;
using AssignmentSystem.Infrastructure.Auth;
using AssignmentSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

// Populate configuration from .env for local runs; a no-op under docker compose, where
// these arrive as real environment variables.
DotEnvLoader.Load(Directory.GetCurrentDirectory());

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// ---------------------------------------------------------------------------------------
// Authentication
// ---------------------------------------------------------------------------------------

// Stops the handler rewriting "sub" into the long WS-Federation nameidentifier URI. Without
// this, the claim the token actually carries is not the claim the code later looks for.
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var jwtOptions = builder.Configuration.GetValidatedJwtOptions();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Take the token's claim types verbatim.
        //
        // Left on, the handler rewrites short names through its inbound map — "role"
        // becomes the WS-Federation ClaimTypes.Role URI — while RoleClaimType below still
        // looks for "role". The claim is present, the lookup misses, and every
        // [Authorize(Roles = ...)] endpoint answers 403 to a perfectly valid token.
        // Clearing JwtSecurityTokenHandler's static map is not enough here: .NET 8 uses
        // JsonWebTokenHandler, which keeps its own.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),

            ValidateLifetime = true,

            // Default is five minutes, which would let an expired token keep working well
            // past its stated expiry — surprising, and it makes expiry untestable.
            ClockSkew = TimeSpan.Zero,

            RoleClaimType = JwtTokenService.RoleClaimType,
            NameClaimType = JwtRegisteredClaimNames.Sub
        };
    });

builder.Services.AddAuthorization();

// ---------------------------------------------------------------------------------------
// Rate limiting on the credential-accepting endpoints
// ---------------------------------------------------------------------------------------

var authPermitPerWindow = int.TryParse(
    builder.Configuration["RateLimit:AuthPermitPerWindow"], out var permit) ? permit : 5;

var authWindowSeconds = int.TryParse(
    builder.Configuration["RateLimit:AuthWindowSeconds"], out var window) ? window : 60;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(AuthController.AuthRateLimitPolicy, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            // Partition by caller address so one attacker cannot lock every user out by
            // exhausting a single shared bucket.
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authPermitPerWindow,
                Window = TimeSpan.FromSeconds(authWindowSeconds),
                QueueLimit = 0
            }));
});

// ---------------------------------------------------------------------------------------
// CORS
// ---------------------------------------------------------------------------------------

const string CorsPolicy = "frontend";

var allowedOrigins = (builder.Configuration["CORS:AllowedOrigins"] ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicy, policy =>
    {
        if (allowedOrigins.Length == 0)
        {
            // No wildcard fallback. An unset origin list means the API refuses browser
            // callers rather than quietly accepting every site on the internet.
            return;
        }

        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    }));

// ---------------------------------------------------------------------------------------
// MVC, validation, docs
// ---------------------------------------------------------------------------------------

builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    // Model-binding failures (malformed JSON, a bad Guid) stay 400: the request could not
    // be understood. FluentValidation failures are 422 and handled by ValidationFilter.
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Type = "about:blank",
            Title = "Malformed request",
            Status = StatusCodes.Status400BadRequest,
            Instance = context.HttpContext.Request.Path
        };

        problem.Extensions["traceId"] =
            Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

        return new BadRequestObjectResult(problem)
        {
            ContentTypes = { "application/problem+json" }
        };
    };
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Assignment & Submission System (evaluation build)",
        Version = "v1",
        Description =
            "Authored by Rakib Hassan for candidacy evaluation. "
            + "Not licensed for production. See LICENSE.",
        Contact = new OpenApiContact
        {
            Name = BuildInfo.Author,
            Email = BuildInfo.Contact
        }
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the accessToken returned by POST /api/v1/auth/login."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlPath = Path.Combine(AppContext.BaseDirectory, "AssignmentSystem.Api.xml");

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Migrate and seed before serving traffic, so a fresh `docker compose up` yields a
// populated, working system with no manual database step.
await DatabaseInitializer.InitialiseAsync(app.Services);

// Outermost, so it catches anything thrown further down the pipeline.
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<BuildSignatureMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(CorsPolicy);

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// Exposed so the integration test project can drive the real application through
/// WebApplicationFactory rather than a stand-in host.
/// </summary>
public partial class Program;
