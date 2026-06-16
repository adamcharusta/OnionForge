# Template manifest

**Copy the files in this directory, do not generate them from scratch.** After copying, replace the placeholders and change nothing else (unless the consultation decisions require it — then use the snippets at the end of this file). Exception: files marked **adaptable** (`Api/Program.cs`, `Api/DependencyInjection.cs`) contain `// EXTEND:` markers — add stack-specific code only at those markers, leave the rest untouched.

Files that start with a dot in the target project are stored here **without the dot** (e.g. `gitignore` → `.gitignore`, `dockerignore` → `.dockerignore`) — add the dot when copying.

**Encoding rule (mandatory):** write every file as UTF-8 **without BOM** and keep **code and config comments ASCII-only** — no em-dashes (`—`), smart quotes or other non-ASCII punctuation in `.cs`/`.ts`/`.xml`/config files. On Windows a misencoded write turns `—` into mojibake (`â€"`). Markdown under `docs/` may use Unicode freely. If you copy templates through PowerShell, force the encoding (`Set-Content -Encoding utf8`); a byte-for-byte copy (`Copy-Item`) is safest.

## Generated project layout

```
{project-name}/
├── .gitignore                 # general: OS + IDE + secrets (from root/gitignore)
├── .gitattributes             # line-ending + encoding normalization (from root/gitattributes)
├── .editorconfig
├── .env / .env.example
├── docker-compose.yml         # runs the whole project: mssql + api + web (+ tools)
├── docs/                      # project documentation in English (architecture, getting started, ADRs)
├── scripts/                   # bash scripts (Sonar scans, helpers)
└── source/
    ├── api/                   # .NET solution (own .gitignore, .dockerignore, Dockerfile, Sonar)
    │   └── openapi/           # OpenAPI document emitted by the Api build (input for Orval)
    └── web/                   # Vite + React (own .gitignore, .dockerignore, Dockerfile, Sonar, Orval)
```

**Credentials rule:** every docker-compose service that supports authentication gets its credentials from `.env` variables (documented in `.env.example`) — never hardcoded defaults like `guest/guest`. `ASPNETCORE_ENVIRONMENT` of the api container is also driven by `.env` (default `Development`).

## Placeholders

| Token | Meaning | Example |
|---|---|---|
| `{{SolutionName}}` | solution name / namespace prefix (PascalCase) | `OrderFlow` |
| `{{solution-name-lower}}` | kebab-case name for container names and Sonar project keys | `orderflow` |
| `{{TargetFramework}}` | current LTS from `dotnet --list-sdks` | `net10.0` |
| `{{DotnetVersion}}` | LTS version for Docker image tags | `10.0` |
| `{{ApiPort}}` | API HTTP port from `launchSettings.json` | `5000` |
| `{{SaPassword}}` | SA password — the same one that goes into `.env` | — |

## Copying — always

### Project root

| Template | Target | Placeholders |
|---|---|---|
| `root/gitignore` | `.gitignore` | — |
| `root/gitattributes` | `.gitattributes` | — |
| `root/editorconfig` | `.editorconfig` | — |
| `root/docker-compose.yml` | `docker-compose.yml` | solution-name-lower, SolutionName, ApiPort |
| `root/env.example` | `.env.example` **and** `.env` (with the real password; `.env` is gitignored) | — |

### Scripts (`scripts/`) — make them executable (`chmod +x`)

| Template | Target | Placeholders |
|---|---|---|
| `scripts/sonar-api.sh` | `scripts/sonar-api.sh` | solution-name-lower, SolutionName |
| `scripts/sonar-web.sh` | `scripts/sonar-web.sh` | — |

### Backend (`source/api/`)

| Template | Target | Placeholders |
|---|---|---|
| `api/gitignore` | `source/api/.gitignore` | — |
| `api/dockerignore` | `source/api/.dockerignore` | — |
| `api/Dockerfile` | `source/api/Dockerfile` | DotnetVersion, SolutionName |
| `api/Directory.Build.props` | `source/api/Directory.Build.props` | TargetFramework |
| `api/SonarQube.Analysis.xml` | `source/api/SonarQube.Analysis.xml` | — |
| `api/coverlet.runsettings` | `source/api/coverlet.runsettings` | — |
| `api/Api/Program.cs` | `source/api/src/{{SolutionName}}.Api/Program.cs` (**adaptable** — extend only at `// EXTEND:` markers) | SolutionName |
| `api/Api/DependencyInjection.cs` | `source/api/src/{{SolutionName}}.Api/DependencyInjection.cs` (**adaptable** — extend only at `// EXTEND:` markers) | SolutionName |
| `api/Domain/Common/Entity.cs` | `source/api/src/{{SolutionName}}.Domain/Common/` | SolutionName |
| `api/Domain/Common/DomainException.cs` | `source/api/src/{{SolutionName}}.Domain/Common/` | SolutionName |
| `api/Application/Abstractions/IDateTimeProvider.cs` | `source/api/src/{{SolutionName}}.Application/Abstractions/` | SolutionName |
| `api/Infrastructure/Time/DateTimeProvider.cs` | `source/api/src/{{SolutionName}}.Infrastructure/Time/` | SolutionName |
| `api/Api/Middleware/GlobalExceptionHandler.cs` | `source/api/src/{{SolutionName}}.Api/Middleware/` | SolutionName |
| `api/Api/appsettings.json` | `source/api/src/{{SolutionName}}.Api/appsettings.json` | SolutionName, SaPassword |
| `api/tests/IntegrationTests/ApiFactory.cs` | `source/api/tests/{{SolutionName}}.IntegrationTests/` | SolutionName |

Sonar for the backend uses **SonarScanner for .NET** driven by `scripts/sonar-api.sh` — it requires a local tool manifest. During generation run inside `source/api/`:

```bash
dotnet new tool-manifest
dotnet tool install dotnet-sonarscanner
```

and add the `coverlet.collector` package to both test projects (coverage for Sonar).

**OpenAPI document on build (input for Orval):** add `Microsoft.Extensions.ApiDescription.Server` (PrivateAssets=all) to the Api project and set in its csproj:

```xml
<PropertyGroup>
  <OpenApiDocumentsDirectory>..\..\openapi</OpenApiDocumentsDirectory>
</PropertyGroup>
```

Every `dotnet build` then refreshes `source/api/openapi/{{SolutionName}}.Api.json`, which `source/web/orval.config.ts` consumes.

### Frontend (`source/web/`)

| Template | Target | Placeholders |
|---|---|---|
| `web/gitignore` | `source/web/.gitignore` | — |
| `web/dockerignore` | `source/web/.dockerignore` | — |
| `web/Dockerfile` | `source/web/Dockerfile` | — |
| `web/nginx.conf` | `source/web/nginx.conf` | — |
| `web/sonar-project.properties` | `source/web/sonar-project.properties` | solution-name-lower, SolutionName |
| `web/orval.config.ts` | `source/web/orval.config.ts` | SolutionName |
| `web/eslint.config.js` | `source/web/eslint.config.js` | — |
| `web/prettierrc` | `source/web/.prettierrc` | — |
| `web/vite.config.ts` | `source/web/vite.config.ts` | ApiPort |
| `web/src/test/setup.ts` | `source/web/src/test/setup.ts` | — |
| `web/src/api/client.ts` | `source/web/src/api/client.ts` | — |

## Copying — conditional

| Template | Condition |
|---|---|
| `api/mediator-custom/Messaging/*.cs` → `source/api/src/{{SolutionName}}.Application/Messaging/` | the **own mediator implementation** was chosen (requires the Scrutor package) |
| `git/husky/task-runner.json` → `.husky/task-runner.json` (placeholder: SolutionName) | tier 1+ **and** hook runner = **Husky.Net** |
| `git/husky/commit-lint.csx` → `.husky/csx/commit-lint.csx` | tier 1+ **and** hook runner = **Husky.Net** |
| `git/husky/commit-msg` → `.husky/commit-msg` | tier 1+ **and** hook runner = **Husky.Net** |
| `git/husky/pre-commit` → `.husky/pre-commit` | tier 1+ **and** hook runner = **Husky.Net** |
| `git/husky-node/package.json` → `package.json` (root; placeholders: solution-name-lower, SolutionName via lintstagedrc) | tier 1+ **and** hook runner = **classic Husky** |
| `git/husky-node/commitlint.config.js` → `commitlint.config.js` | tier 1+ **and** hook runner = **classic Husky** |
| `git/husky-node/lintstagedrc.mjs` → `.lintstagedrc.mjs` (placeholder: SolutionName) | tier 1+ **and** hook runner = **classic Husky** |
| `git/husky-node/commit-msg` → `.husky/commit-msg` | tier 1+ **and** hook runner = **classic Husky** |
| `git/husky-node/pre-commit` → `.husky/pre-commit` | tier 1+ **and** hook runner = **classic Husky** |
| MinVer snippet (see [references/versioning.md](../references/versioning.md)) → `source/api/Directory.Build.props` + `Directory.Packages.props` | commit & versioning **tier 2+** chosen |
| `git/release-please/workflow.yml` → `.github/workflows/release-please.yml` | commit & versioning **tier 3** chosen |
| `git/release-please/release-please-config.json` → `release-please-config.json` (placeholder: solution-name-lower) | commit & versioning **tier 3** chosen |
| `git/release-please/release-please-manifest.json` → `.release-please-manifest.json` | commit & versioning **tier 3** chosen |

Full per-tier setup (Husky install commands, MinVer snippet, scaffold-commit `--no-verify`): [references/versioning.md](../references/versioning.md).

## Variant snippets

### Second Serilog sink — append to `"WriteTo"` in appsettings.json

Seq (package `Serilog.Sinks.Seq`):

```json
{ "Name": "Seq", "Args": { "serverUrl": "http://localhost:5341" } }
```

Rolling file (package `Serilog.Sinks.File`):

```json
{ "Name": "File", "Args": { "path": "logs/log-.txt", "rollingInterval": "Day", "retainedFileCountLimit": 14 } }
```

MSSQL (package `Serilog.Sinks.MSSqlServer`):

```json
{ "Name": "MSSqlServer", "Args": { "connectionString": "<same as ConnectionStrings:Database>", "sinkOptionsSection": { "tableName": "Logs", "autoCreateSqlTable": true } } }
```

⚠ This sink hardcodes its own `connectionString` in `appsettings.json` — it does **not** reuse `ConnectionStrings:Database`. The `appsettings.json` value points at `localhost`, which is correct only when the api runs on the host. When the api runs in the compose network, override it there too (`Serilog__WriteTo__1__Args__connectionString` pointing at `Server=mssql;...`), exactly like the `ConnectionStrings__Database` override — otherwise the sink silently fails to write logs from inside Docker.

Grafana Loki (package `Serilog.Sinks.Grafana.Loki`):

```json
{ "Name": "GrafanaLoki", "Args": { "uri": "http://localhost:3100" } }
```

⚠ Loki is a **separate server**, like Seq — picking this sink requires adding the Loki + Grafana services to `docker-compose.yml` (snippet below), or logs go nowhere. In-network the api targets `http://loki:3100`; on the host it is `http://localhost:3100`.

### Services with credentials — append to `docker-compose.yml` when chosen

Whenever one of these is added, also add its variables to `.env` **and** `.env.example`, and use the same variables in the api connection strings (never hardcode `guest/guest` or empty passwords).

RabbitMQ (variables: `RABBITMQ_USER`, `RABBITMQ_PASSWORD`; api connection string: `amqp://${RABBITMQ_USER}:${RABBITMQ_PASSWORD}@rabbitmq:5672`; management UI on http://localhost:15672):

```yaml
  rabbitmq:
    image: rabbitmq:4-management-alpine
    container_name: {{solution-name-lower}}-rabbitmq
    restart: unless-stopped
    environment:
      RABBITMQ_DEFAULT_USER: ${RABBITMQ_USER}
      RABBITMQ_DEFAULT_PASS: ${RABBITMQ_PASSWORD}
    ports:
      - "5672:5672"
      - "15672:15672"
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "-q", "ping"]
      interval: 10s
      timeout: 5s
      retries: 10
    volumes:
      - rabbitmq-data:/var/lib/rabbitmq
```

(add `rabbitmq-data` to the top-level `volumes:`) The volume keeps durable queues and undelivered messages across restarts; without it every `docker compose down`/recreate empties the broker. The management UI (`http://localhost:15672`) authenticates with `RABBITMQ_USER`/`RABBITMQ_PASSWORD`, so it is reachable from the host — no extra auth filter needed.

Redis (variable: `REDIS_PASSWORD`; api connection string: `redis:6379,password=${REDIS_PASSWORD}`):

```yaml
  redis:
    image: redis:7-alpine
    container_name: {{solution-name-lower}}-redis
    restart: unless-stopped
    command: ["redis-server", "--requirepass", "${REDIS_PASSWORD}"]
    ports:
      - "6379:6379"
```

No volume here is intentional: Redis is used as a cache, so losing its contents on restart is harmless (the app repopulates it). If the consultation makes Redis a source of truth rather than a cache, add `redis-data:/data` plus a persistence flag (`--appendonly yes`).

### Seq service — append to `docker-compose.yml` when Seq was chosen

```yaml
  seq:
    image: datalust/seq:2025.2
    container_name: {{solution-name-lower}}-seq
    restart: unless-stopped
    environment:
      ACCEPT_EULA: "Y"
    ports:
      - "5341:80"
    volumes:
      - seq-data:/data
```

(remember to add `seq-data` to the top-level `volumes:`)

**The `seq-data` volume is mandatory, not optional.** Seq keeps its Flare/LMDB storage in `/data`; without a persistent volume an unclean container shutdown (a plain `docker compose down`/recreate) corrupts that storage and Seq then crashes on the next start with an Autofac `StorageSubsystem` activation error. Pin a concrete version (not `latest`) for reproducible builds.

Seq listens on **port 80** (UI + API + ingestion) and on **5341** (ingestion-only) inside the container; `5341:80` maps the host's `5341` to the container UI. So the Serilog `serverUrl` differs by where the api runs:

- **api on the host** (local dev, default `appsettings.json`): `http://localhost:5341`.
- **api in the compose network** (override in the api service env): `http://seq` (the in-network UI/ingestion port 80) — set it with `Serilog__WriteTo__1__Args__serverUrl: "http://seq"` on the `api` service. Do **not** use `http://seq:5341` here; that targets the ingestion-only port, not the one the host mapping documents.

### SonarQube server — append to `docker-compose.yml` when the user wants a local Sonar instance

Ask during the consultation whether the team has an existing SonarQube/SonarCloud instance or wants a local container (note: resource-heavy, ~2 GB RAM):

```yaml
  sonarqube:
    image: sonarqube:community
    container_name: {{solution-name-lower}}-sonarqube
    environment:
      SONAR_ES_BOOTSTRAP_CHECKS_DISABLE: "true"
    ports:
      - "9000:9000"
    volumes:
      - sonarqube-data:/opt/sonarqube/data
      - sonarqube-extensions:/opt/sonarqube/extensions
```

(remember to add `sonarqube-data` and `sonarqube-extensions` to `volumes:`)

### Grafana Loki stack — append to `docker-compose.yml` when the Grafana Loki sink was chosen

The Loki sink needs a Loki server to receive logs and Grafana to read them (Loki has no UI of its own). Grafana is exposed on host port **3001** because the web app already uses 3000:

```yaml
  loki:
    image: grafana/loki:3.5
    container_name: {{solution-name-lower}}-loki
    restart: unless-stopped
    command: -config.file=/etc/loki/local-config.yaml
    ports:
      - "3100:3100"
    volumes:
      - loki-data:/loki

  grafana:
    image: grafana/grafana:11.6
    container_name: {{solution-name-lower}}-grafana
    restart: unless-stopped
    environment:
      GF_SECURITY_ADMIN_PASSWORD: ${GRAFANA_PASSWORD}
    ports:
      - "3001:3000"
    depends_on:
      - loki
    volumes:
      - grafana-data:/var/lib/grafana
```

(add `loki-data` and `grafana-data` to the top-level `volumes:`, and `GRAFANA_PASSWORD` to `.env` + `.env.example`). Grafana UI on `http://localhost:3001` (login `admin` / `GRAFANA_PASSWORD`); add Loki as a data source pointed at `http://loki:3100`. List both in `README.md` and `docs/development.md`.

### Background processing — Hangfire — when Hangfire was chosen

Hangfire reuses the **existing MSSQL** for job storage, so it needs **no extra docker-compose service** — only packages, DI registration and a dashboard mapping. Packages (add to `Directory.Packages.props` + the Infrastructure `.csproj`): `Hangfire.AspNetCore`, `Hangfire.SqlServer`.

**Infrastructure `DependencyInjection.cs`** — register the storage and the server (gate them behind `Hangfire:Enabled` so integration tests and build-time OpenAPI generation, which have no live SQL, can switch them off):

```csharp
// AddInfrastructure(...), after the DbContext is registered:
if (configuration.GetValue("Hangfire:Enabled", true))
{
    services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
        {
            PrepareSchemaIfNecessary = true,
            QueuePollInterval = TimeSpan.FromSeconds(15),
        }));
    services.AddHangfireServer();
}
```

**Api `DependencyInjection.cs`** — map the dashboard at the `// EXTEND:` pipeline marker in `UseApi`. The dashboard must be mapped with an **explicit authorization filter**: the default `MapHangfireDashboard("/hangfire")` applies `LocalRequestsOnlyAuthorizationFilter`, which returns **403** for every request that is not local — so when the api runs in Docker and you open the dashboard from the host browser, it is blocked. That is the usual "no access to Hangfire" symptom. Map it only in Development with a filter that allows the request:

```csharp
if (app.Environment.IsDevelopment() && app.Configuration.GetValue("Hangfire:Enabled", true))
{
    app.MapHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new AllowAllDashboardAuthorizationFilter()],
    });
}
```

```csharp
// Api/Dashboards/AllowAllDashboardAuthorizationFilter.cs
using Hangfire.Dashboard;

namespace {{SolutionName}}.Api.Dashboards;

// Development only: the dashboard is mapped solely under IsDevelopment(), so allowing
// every request makes it reachable from the host browser when the api runs in Docker
// (the default LocalRequestsOnlyAuthorizationFilter blocks non-local requests). For a
// Production dashboard, replace this with a real auth check.
internal sealed class AllowAllDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}
```

**`appsettings.json`** — add the toggle the registrations read:

```json
"Hangfire": { "Enabled": true }
```

Schedule recurring jobs (`RecurringJob.AddOrUpdate`) from an `IHostedService` that runs at startup, not during build-time OpenAPI generation — see the `Hangfire:Enabled` gating pattern in [../references/backend.md](../references/backend.md). The dashboard URL (`/hangfire`) goes in `README.md` and `docs/development.md`.

### Frontend `package.json` scripts — set exactly like this

```json
{
  "predev": "npm run api:generate",
  "dev": "vite",
  "build": "tsc -b && vite build",
  "api:generate": "orval",
  "lint": "eslint .",
  "format": "prettier --write .",
  "test": "vitest run",
  "test:watch": "vitest",
  "coverage": "vitest run --coverage"
}
```

`api:generate` regenerates the typed client from `source/api/openapi/` (requires the `orval` devDependency); it also runs automatically before `npm run dev`. The generated `src/api/generated/` output is committed, so `npm run build` (and the Docker image build) works without the backend spec — do **not** hook orval into `prebuild`.
