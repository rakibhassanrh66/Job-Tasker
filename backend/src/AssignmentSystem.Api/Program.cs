// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Api.Configuration;
using AssignmentSystem.Infrastructure;
using AssignmentSystem.Infrastructure.Persistence;

// Populate configuration from .env for local runs; a no-op under docker compose, where
// these arrive as real environment variables.
DotEnvLoader.Load(Directory.GetCurrentDirectory());

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Migrate and seed before serving traffic, so a fresh `docker compose up` yields a
// populated, working system with no manual database step.
await DatabaseInitializer.InitialiseAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// Exposed so the integration test project can drive the real application through
/// WebApplicationFactory rather than a stand-in host.
/// </summary>
public partial class Program;
