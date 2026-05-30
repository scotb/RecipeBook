# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# Copy central build/package props first (layer-cached until they change)
COPY Directory.Build.props Directory.Packages.props ./

# Copy project files for restore (layer-cached until deps change)
COPY RecipeBook.Domain/RecipeBook.Domain.csproj             RecipeBook.Domain/
COPY RecipeBook.Application/RecipeBook.Application.csproj   RecipeBook.Application/
COPY RecipeBook.Infrastructure/RecipeBook.Infrastructure.csproj RecipeBook.Infrastructure/
COPY RecipeBook.Api/RecipeBook.Api.csproj                   RecipeBook.Api/

RUN dotnet restore RecipeBook.Api/RecipeBook.Api.csproj

# Copy source and publish
COPY RecipeBook.Domain/        RecipeBook.Domain/
COPY RecipeBook.Application/   RecipeBook.Application/
COPY RecipeBook.Infrastructure/ RecipeBook.Infrastructure/
COPY RecipeBook.Api/           RecipeBook.Api/

RUN dotnet publish RecipeBook.Api/RecipeBook.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Runtime image ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Run as non-root
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "RecipeBook.Api.dll"]
