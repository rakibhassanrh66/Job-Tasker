# Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
# Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa
#
# Build context is ./backend (see docker-compose.yml).

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy manifests first so `restore` is cached independently of source changes.
COPY Directory.Build.props ./
COPY AssignmentSystem.sln ./
COPY src/AssignmentSystem.Domain/*.csproj          src/AssignmentSystem.Domain/
COPY src/AssignmentSystem.Application/*.csproj     src/AssignmentSystem.Application/
COPY src/AssignmentSystem.Infrastructure/*.csproj  src/AssignmentSystem.Infrastructure/
COPY src/AssignmentSystem.Api/*.csproj             src/AssignmentSystem.Api/
COPY tests/AssignmentSystem.UnitTests/*.csproj         tests/AssignmentSystem.UnitTests/
COPY tests/AssignmentSystem.IntegrationTests/*.csproj  tests/AssignmentSystem.IntegrationTests/
RUN dotnet restore AssignmentSystem.sln

COPY . .
RUN dotnet publish src/AssignmentSystem.Api/AssignmentSystem.Api.csproj \
    -c Release -o /app/publish --no-restore

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Run as a non-root user.
RUN adduser --disabled-password --gecos "" --uid 5678 appuser && chown -R appuser /app
USER appuser

COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AssignmentSystem.Api.dll"]
