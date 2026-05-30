# PRD 07 — Deployment & Infrastructure

## 1. Overview

RecipeBook runs in two environments:

| Environment | Purpose | Hosting |
|---|---|---|
| **Local development** | Engineering, testing, agentic development sessions | Docker Compose on the developer's machine |
| **Production** | Live public demo (portfolio showcase) | Microsoft Azure |

---

## 2. Local Development (Docker Compose)

### 2.1 Services

```yaml
# docker-compose.yml (at repository root)
services:

  db:
    image: postgres:17
    environment:
      POSTGRES_DB: recipebook
      POSTGRES_USER: recipebook
      POSTGRES_PASSWORD: recipebook_dev   # dev-only, never use in prod
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U recipebook"]
      interval: 5s
      timeout: 5s
      retries: 5

  api:
    build:
      context: .
      dockerfile: RecipeBook.Api/Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      Database__Provider: PostgreSQL
      ConnectionStrings__Default: "Host=db;Database=recipebook;Username=recipebook;Password=recipebook_dev"
      Jwt__Issuer: https://localhost:7100
      Jwt__Audience: https://localhost:7200
      Jwt__ExpiryMinutes: 60
      # Secrets below must be provided via .env file (never committed to source control)
      Jwt__SecretKey: ${JWT_SECRET_KEY}
      Authentication__Google__ClientId: ${GOOGLE_CLIENT_ID}
      Authentication__Google__ClientSecret: ${GOOGLE_CLIENT_SECRET}
      Authentication__Facebook__AppId: ${FACEBOOK_APP_ID}
      Authentication__Facebook__AppSecret: ${FACEBOOK_APP_SECRET}
      Admin__SeedEmails: ${ADMIN_SEED_EMAILS}
    ports:
      - "7100:8080"
    depends_on:
      db:
        condition: service_healthy

  blazor:
    build:
      context: .
      dockerfile: RecipeBook.Blazor.Server/Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      Api__BaseUrl: http://api:8080
    ports:
      - "7200:8080"
    depends_on:
      - api

volumes:
  postgres_data:
```

### 2.2 `.env` File (Developer Machine)

A `.env` file at the repository root is used for secrets in local development. This file is in `.gitignore` and **never committed**.

```
# .env.example (committed — no real values)
JWT_SECRET_KEY=replace-with-32-char-random-string
GOOGLE_CLIENT_ID=
GOOGLE_CLIENT_SECRET=
FACEBOOK_APP_ID=
FACEBOOK_APP_SECRET=
ADMIN_SEED_EMAILS=your@email.com
```

Developers copy `.env.example` to `.env` and populate their own credentials.

### 2.3 Running Locally

```bash
# First time or after schema changes:
docker compose up db -d
dotnet ef database update --project RecipeBook.Infrastructure --startup-project RecipeBook.Api

# Run everything:
docker compose up --build

# Run tests only (no containers needed for Domain/Application tests):
dotnet test RecipeBook.Domain.Tests
dotnet test RecipeBook.Application.Tests

# Infrastructure tests (requires Docker for Testcontainers):
dotnet test RecipeBook.Infrastructure.Tests

# All tests:
dotnet test RecipeBook.slnx
```

---

## 3. Docker Configuration

### 3.1 API Dockerfile

```dockerfile
# RecipeBook.Api/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY Directory.Build.props Directory.Packages.props ./
COPY ["RecipeBook.Api/RecipeBook.Api.csproj", "RecipeBook.Api/"]
COPY ["RecipeBook.Application/RecipeBook.Application.csproj", "RecipeBook.Application/"]
COPY ["RecipeBook.Infrastructure/RecipeBook.Infrastructure.csproj", "RecipeBook.Infrastructure/"]
COPY ["RecipeBook.Domain/RecipeBook.Domain.csproj", "RecipeBook.Domain/"]
RUN dotnet restore "RecipeBook.Api/RecipeBook.Api.csproj"
COPY . .
RUN dotnet publish "RecipeBook.Api/RecipeBook.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "RecipeBook.Api.dll"]
```

### 3.2 Blazor Dockerfile

```dockerfile
# RecipeBook.Blazor.Server/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY Directory.Build.props Directory.Packages.props ./
COPY ["RecipeBook.Blazor.Server/RecipeBook.Blazor.Server.csproj", "RecipeBook.Blazor.Server/"]
COPY ["RecipeBook.Application/RecipeBook.Application.csproj", "RecipeBook.Application/"]
COPY ["RecipeBook.Domain/RecipeBook.Domain.csproj", "RecipeBook.Domain/"]
RUN dotnet restore "RecipeBook.Blazor.Server/RecipeBook.Blazor.Server.csproj"
COPY . .
RUN dotnet publish "RecipeBook.Blazor.Server/RecipeBook.Blazor.Server.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "RecipeBook.Blazor.Server.dll"]
```

---

## 4. Azure Production Infrastructure

### 4.1 Resource List

| Resource | Azure Service | SKU (starting) | Purpose |
|---|---|---|---|
| API | Azure App Service | B1 | Hosts `RecipeBook.Api` |
| Blazor UI | Azure App Service | B1 | Hosts `RecipeBook.Blazor.Server` |
| Database | Azure Database for PostgreSQL Flexible Server | Burstable B1ms | Managed PostgreSQL 17 |
| Secrets | Azure Key Vault | Standard | All secrets and connection strings |
| Container Registry | Azure Container Registry | Basic | Docker image storage |
| CI/CD | GitHub Actions | Free tier | Build, test, deploy pipeline |

### 4.2 Domain Names (Example)

| Endpoint | URL |
|---|---|
| API | `https://api.recipebook.scottsmith.dev` |
| Blazor UI | `https://recipebook.scottsmith.dev` |

Update with actual domain before go-live.

### 4.3 Key Vault Secrets

| Secret Name | Description |
|---|---|
| `Jwt--SecretKey` | JWT signing key |
| `Authentication--Google--ClientId` | Google OAuth client ID |
| `Authentication--Google--ClientSecret` | Google OAuth client secret |
| `Authentication--Facebook--AppId` | Facebook app ID |
| `Authentication--Facebook--AppSecret` | Facebook app secret |
| `ConnectionStrings--Default` | PostgreSQL connection string |
| `Admin--SeedEmails` | Comma-separated admin email list |

Secrets use `--` as the Key Vault naming separator (maps to `:` in .NET configuration).

### 4.4 App Service Configuration

App Services are configured to use **Managed Identity** to access Key Vault. No secrets are stored in App Service Application Settings directly.

```
ASPNETCORE_ENVIRONMENT = Production
AZURE_KEY_VAULT_URI = https://recipebook-kv.vault.azure.net/
Database__Provider = PostgreSQL
```

---

## 5. CI/CD Pipeline (GitHub Actions)

### 5.1 On Pull Request

```yaml
# .github/workflows/pr.yml
name: PR Checks
on:
  pull_request:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:17
        env:
          POSTGRES_DB: recipebook_test
          POSTGRES_USER: recipebook
          POSTGRES_PASSWORD: test
        ports:
          - 5432:5432
        options: >-
          --health-cmd pg_isready
          --health-interval 10s

    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet restore RecipeBook.slnx
      - run: dotnet build RecipeBook.slnx --no-restore
      - run: dotnet test RecipeBook.slnx --no-build --logger trx
        env:
          TEST_DB_CONNECTION: "Host=localhost;Database=recipebook_test;Username=recipebook;Password=test"
```

### 5.2 On Merge to Main (Deploy)

```yaml
# .github/workflows/deploy.yml
name: Deploy to Azure
on:
  push:
    branches: [main]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet test RecipeBook.slnx   # Tests must pass before deploy

      - name: Build and push API image
        run: |
          az acr login --name recipebookacr
          docker build -t recipebookacr.azurecr.io/recipebook-api:${{ github.sha }} -f RecipeBook.Api/Dockerfile .
          docker push recipebookacr.azurecr.io/recipebook-api:${{ github.sha }}

      - name: Deploy API to App Service
        uses: azure/webapps-deploy@v3
        with:
          app-name: recipebook-api
          images: recipebookacr.azurecr.io/recipebook-api:${{ github.sha }}

      - name: Build and push Blazor image
        run: |
          docker build -t recipebookacr.azurecr.io/recipebook-blazor:${{ github.sha }} -f RecipeBook.Blazor.Server/Dockerfile .
          docker push recipebookacr.azurecr.io/recipebook-blazor:${{ github.sha }}

      - name: Deploy Blazor to App Service
        uses: azure/webapps-deploy@v3
        with:
          app-name: recipebook-blazor
          images: recipebookacr.azurecr.io/recipebook-blazor:${{ github.sha }}
```

---

## 6. Database Migration Strategy

- Migrations are run as part of application startup in development (`Database.MigrateAsync()` in `Program.cs` when `ASPNETCORE_ENVIRONMENT = Development`).
- In production, migrations are run as a separate pre-deploy step via a GitHub Actions job that runs `dotnet ef database update` using the production connection string (sourced from Key Vault via a service principal with limited access).
- **Never** run `Database.EnsureCreated()` in any environment — always use migrations.

---

## 7. Environment Summary

| Config Key | Development | Production |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Development` | `Production` |
| `Database__Provider` | `PostgreSQL` | `PostgreSQL` |
| Database host | `localhost:5432` (Docker) | Azure Flexible Server |
| Secrets source | `.env` file | Azure Key Vault |
| Swagger UI | Enabled | Disabled |
| HTTPS redirect | Optional (local) | Enforced |
| Detailed error pages | Enabled | Disabled |
| Log level | `Debug` | `Warning` |

---

## 8. Monitoring & Logging (V1 Baseline)

| Concern | V1 Approach |
|---|---|
| Structured logging | `Microsoft.Extensions.Logging` with `Serilog` sink to console (structured JSON in production) |
| Log shipping | Azure App Service streams logs to Azure Monitor Logs (built-in) |
| Health check endpoint | `GET /health` via `app.MapHealthChecks()` — checks DB connectivity |
| Alerting | Azure Monitor alert on 5xx rate or App Service health check failure |
| Performance monitoring | Application Insights basic telemetry (free tier) |

Full observability (distributed tracing, custom metrics, dashboards) is a V2 concern.
