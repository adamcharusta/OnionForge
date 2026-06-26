# Backend — generation conventions (.NET, Onion Architecture)

Target framework: the newest .NET LTS available on the machine (`dotnet --list-sdks`). Database: **always MSSQL**.

## Project layout

The generated repository follows a fixed layout (full map in [../templates/MANIFEST.md](../templates/MANIFEST.md)); the whole .NET solution lives under `source/api/`:

```
{project-name}/
├── .gitignore                     # general: OS + IDE + secrets
├── .editorconfig
├── .env / .env.example
├── docker-compose.yml             # whole project: mssql + api + web (+ tools: Seq, SonarQube)
├── docs/                          # project documentation in English (architecture, getting started, ADRs)
├── scripts/                       # bash scripts: sonar-api.sh, sonar-web.sh
└── source/
    ├── api/
    │   ├── {Name}.slnx                    # XML solution file (see "Solution file" below)
    │   ├── Directory.Build.props          # shared settings for all projects
    │   ├── Directory.Packages.props       # central package management
    │   ├── .gitignore / .dockerignore / Dockerfile
    │   ├── .config/dotnet-tools.json      # dotnet-sonarscanner local tool
    │   ├── SonarQube.Analysis.xml         # scanner settings (the .NET counterpart of sonar-project.properties)
    │   ├── coverlet.runsettings           # shared coverage settings (OpenCover) for dotnet test
    │   ├── openapi/                       # OpenAPI document emitted by the Api build (input for Orval)
    │   ├── src/
    │   │   ├── {Name}.Domain/             # entities, value objects, enums, domain exceptions
    │   │   ├── {Name}.Application/        # use cases, DTOs, validators, interfaces (IRepository, I*Service)
    │   │   ├── {Name}.Infrastructure/     # implementations: data access, integrations, clock, e-mail etc.
    │   │   └── {Name}.Api/                # endpoints, middleware, DI composition, appsettings
    │   └── tests/
    │       ├── {Name}.UnitTests/          # xUnit + NSubstitute + FluentAssertions 7.x
    │       ├── {Name}.IntegrationTests/   # Testcontainers.MsSql + WebApplicationFactory
    │       └── {Name}.ArchitectureTests/  # NetArchTest.Rules — Onion dependency rules (mandatory)
    └── web/                               # frontend — see frontend.md
```

Dependency rules (enforced via project references):

- `Domain` → nothing (zero NuGet packages besides analyzers),
- `Application` → `Domain`,
- `Infrastructure` → `Application`,
- `Api` → `Application` + `Infrastructure` (for DI registration only).

## Solution file (`.slnx`)

Generate `source/api/{Name}.slnx` (the modern XML solution format) from scratch with **three folders**:

- `/src/` — the four layer projects (`Domain`, `Application`, `Infrastructure`, `Api`).
- `/tests/` — the test projects (`UnitTests`, `IntegrationTests`).
- `/solutionItems/` — the api-root **non-project files**, so they are visible and editable in the
  IDE solution explorer (Rider / Visual Studio) instead of being hidden on disk: `.dockerignore`,
  `.gitignore`, `Directory.Build.props`, `Directory.Packages.props`, `Dockerfile`,
  `SonarQube.Analysis.xml`, `coverlet.runsettings`.

```xml
<Solution>
  <Folder Name="/solutionItems/">
    <File Path=".dockerignore" />
    <File Path=".gitignore" />
    <File Path="Directory.Build.props" />
    <File Path="Directory.Packages.props" />
    <File Path="Dockerfile" />
    <File Path="SonarQube.Analysis.xml" />
    <File Path="coverlet.runsettings" />
  </Folder>
  <Folder Name="/src/">
    <Project Path="src/{Name}.Api/{Name}.Api.csproj" />
    <Project Path="src/{Name}.Application/{Name}.Application.csproj" />
    <Project Path="src/{Name}.Domain/{Name}.Domain.csproj" />
    <Project Path="src/{Name}.Infrastructure/{Name}.Infrastructure.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/{Name}.IntegrationTests/{Name}.IntegrationTests.csproj" />
    <Project Path="tests/{Name}.UnitTests/{Name}.UnitTests.csproj" />
  </Folder>
</Solution>
```

Add any extra api-root file the consultation introduces (e.g. another `*.props`) to `/solutionItems/`,
and any extra project (e.g. a Worker) to `/src/`.

## Invariant files — copy from templates

`Directory.Build.props`, `.editorconfig`, root and api `.gitignore`, `.dockerignore`, `Dockerfile`, `docker-compose.yml`, `.env.example`, `appsettings.json`, `SonarQube.Analysis.xml`, `coverlet.runsettings`, the Sonar scripts, `Entity`, `DomainException`, `IDateTimeProvider`/`DateTimeProvider`, `GlobalExceptionHandler`, `ApiFactory` and the custom mediator files are ready in [../templates/](../templates/). Copy them according to [../templates/MANIFEST.md](../templates/MANIFEST.md) (placeholders and variant snippets are documented there) — do not write them from scratch. `Api/Program.cs` and `Api/DependencyInjection.cs` are **adaptable** templates: copy them and add stack-specific code only at the `// EXTEND:` markers.

## Layers — what to generate

### Domain

- Entities with private setters and state-changing methods (behavior inside the entity, not an anemic model).
- `Entity` base class (Id, plus audit fields: CreatedAtUtc/ModifiedAtUtc).
- Domain exceptions inheriting from `DomainException`.

### Application

- Per-feature structure: `Features/{Entity}/{Commands|Queries}/...` — each use case in its own directory (handler + request + validator + DTO side by side).
- Port interfaces: `I{Entity}Repository`, `IUnitOfWork`, `IDateTimeProvider` etc. in `Abstractions/`.
- A FluentValidation validator for every command; registered via `AddValidatorsFromAssembly`.
- Validation runs in the mediator pipeline (behavior/middleware), not inside handlers.
- `DependencyInjection.cs` with an `AddApplication(this IServiceCollection)` method.

### Mediator variant

- **Wolverine** — handlers as plain methods, validation via the built-in FluentValidation integration.
- **MediatR** — `IRequest`/`IRequestHandler` + `ValidationBehavior<,>`. ⚠ Make sure the user has accepted the commercial license.
- **Own implementation** — copy the ready-made files from [../templates/backend/mediator-custom/](../templates/backend/mediator-custom/) into `Application/Messaging/`: `ICommand`/`IQuery` contracts + handlers, the FluentValidation decorator, assembly-scanning registration (requires the `Scrutor` package, MIT).

### Infrastructure

- **EF Core:** `AppDbContext`, entity configurations in separate `IEntityTypeConfiguration<>` classes, an initial migration generated (`dotnet ef migrations add Initial`), repository implementations, `IUnitOfWork` backed by `SaveChangesAsync`.
- **Dapper:** `IDbConnectionFactory` (SqlConnection), repositories with explicit SQL, schema scripts in `Database/Scripts/` executed by DbUp/grate at startup or via a separate command.
- `DependencyInjection.cs` with `AddInfrastructure(this IServiceCollection, IConfiguration)`.

### Api

- **`DependencyInjection.cs` like every other layer** (adaptable template): `AddApi(IServiceCollection, IConfiguration)` holds all Api-layer registrations, `UseApi(WebApplication)` holds the whole pipeline + endpoint mapping. `Program.cs` (adaptable template) stays thin: Serilog + `AddApplication()`/`AddInfrastructure()`/`AddApi()` + host-level config (e.g. `UseWolverine`) + `UseApi()`.
- Minimal APIs grouped per feature (`MapGroup`) — unless the user asks for controllers.
- Global error handling: `IExceptionHandler` → ProblemDetails; mapping `ValidationException` → 400 with the error list, `DomainException` → 422, everything else → 500 without details.
- Serilog: `UseSerilog` configured from `appsettings.json`; Console sink always + the chosen second sink; request logging (`UseSerilogRequestLogging`).
- OpenAPI + Scalar/Swagger UI in Development. The OpenAPI document is also **emitted at build time** to `source/api/openapi/` via `Microsoft.Extensions.ApiDescription.Server` (csproj setup in MANIFEST.md) — the frontend's Orval client is generated from it.
- **Every tool with a web UI** (Scalar/Swagger, Hangfire dashboard, etc.) must be reachable when running in Development and listed with its URL in the README and `docs/development.md`. Map dashboards only under `IsDevelopment()`. A dashboard with local-only default authorization (the **Hangfire dashboard** uses `LocalRequestsOnlyAuthorizationFilter`) returns 403 from a host browser when the api runs in Docker — map it with an explicit Development authorization filter (snippet in [../templates/MANIFEST.md](../templates/MANIFEST.md)).
- CORS policy for the frontend origin (from configuration).
- Health check: `/health` (+ a database check).
- `appsettings.json` + `appsettings.Development.json` with the MSSQL connection string matching docker-compose.

## Scheduled & background jobs

When the analysis selects a scheduler (**Hangfire**, **Quartz.NET** or a custom `BackgroundService`) and the app has recurring/cyclical work, the **schedule is configuration, never hardcoded**:

- Put each recurring job's **cron expression** (or interval) in `appsettings.json` under a `Jobs`/`Scheduling` section, with a developer-friendly default, and let it be overridden by an environment variable in docker-compose (`Jobs__<JobName>__Cron`, sourced from `.env`). Register the recurring jobs by reading those values, e.g. `RecurringJob.AddOrUpdate(name, () => ..., cron)` for Hangfire or a `CronScheduleBuilder` for Quartz.
- Document every job's config key and its `.env` variable in `.env.example` and `docs/development.md`, so the schedule (and enabling/disabling a job) can be changed without recompiling.
- Use standard cron syntax and note in the docs which dialect/timezone applies (Hangfire and Quartz differ — Quartz uses a 6/7-field cron and an explicit timezone; state the one in use).
- Hangfire's dashboard counts as a tool UI — expose it in Development per the Api rules (with the explicit Development authorization filter so it opens from the host browser).

## Docker

- Root `docker-compose.yml` (template: `../templates/root/docker-compose.yml`) runs the **whole project**: MSSQL with a healthcheck, the API (built from `source/api/Dockerfile`) and the web frontend (built from `source/web/Dockerfile`). If RabbitMQ, Redis, Seq or a local SonarQube was chosen — append the services from the snippets in MANIFEST.md.
- **Stateful tool containers need a persistent volume**, or they crash on restart. Seq keeps its Flare/LMDB storage in `/data`; without a `seq-data:/data` volume an unclean shutdown corrupts it and Seq then fails to boot with an Autofac `StorageSubsystem` error. Pin concrete image tags (not `latest`) for these tools. The MANIFEST snippets already include the required volumes — keep them.
- **All credentials and the runtime environment come from `.env`** (gitignored, with a provided `.env.example`): the SA password, credentials of every added service (RabbitMQ user/password, Redis password...) and `ASPNETCORE_ENVIRONMENT` of the api container (`${ASPNETCORE_ENVIRONMENT:-Development}` — switchable without editing the compose file). Never hardcode defaults like `guest/guest`.
- `source/api/Dockerfile` is a multi-stage build (SDK → aspnet runtime, port 8080); `source/api/.dockerignore` keeps build context lean. Both are templates — copy, do not write.

### Docker-readiness rules for every chosen service

Apply these to **any** service you add to the compose file, including ones that have no ready snippet (a self-hosted IdP like Keycloak, a search engine, etc.) — the result must run cleanly from a single `docker compose up`:

1. **Stateful → persistent volume.** Anything that stores data (Seq, RabbitMQ, SonarQube, Loki, Grafana, Keycloak's DB...) gets a named volume, or it loses data or corrupt-crashes on the next restart. A pure cache (Redis as a cache) is the only exception.
2. **Pin the image tag** to a concrete version (`datalust/seq:2025.2`, not `:latest`) so the build is reproducible and a server crash is never just "latest changed under us".
3. **The api addresses other services by their compose service name, not `localhost`.** `appsettings.json` carries the `localhost` form (correct when the api runs on the host); the compose `api` service overrides each address to the in-network name via env vars (`ConnectionStrings__Database` → `Server=mssql;...`, `Serilog__WriteTo__1__Args__serverUrl` → `http://seq`, the Loki `uri` → `http://loki:3100`, etc.). A `localhost` address that is not overridden silently fails from inside the container — this hits log sinks (Seq/Loki/MSSQL) the hardest because they fail quietly.
4. **Every tool UI must actually open in Development** (see the Api section): expose its port, and if its dashboard defaults to local-only authorization (Hangfire), map it with an explicit Development auth filter so it is reachable from the host browser when the api runs in Docker.
5. **Credentials from `.env`** (next bullet) for every service that authenticates, mirrored in `.env.example`.

### Service URLs section in the generated README

The generated `README.md` **must** contain a **"Service URLs"** table (repeat it, or link to it, from `docs/development.md`). Build it from the project's own `docker-compose.yml` and `appsettings.json` — one row per service/UI that the chosen stack actually includes, never a row for a tool that was not selected. This is what makes the output ready-to-use: one place with every link and where its credentials come from. Use this canonical catalog and keep only the applicable rows (`{ApiPort}` = the api port from `launchSettings.json`/compose):

| Service / UI | URL (Development) | Credentials |
|---|---|---|
| Web app (frontend) | `http://localhost:3000` (Docker) · `http://localhost:5173` (vite dev) | — |
| API | `http://localhost:{ApiPort}` | — |
| OpenAPI / Scalar reference | `http://localhost:{ApiPort}/scalar/v1` | — |
| Health check | `http://localhost:{ApiPort}/health` | — |
| Hangfire dashboard | `http://localhost:{ApiPort}/hangfire` | open in Development (AllowAll dashboard filter) |
| Seq | `http://localhost:5341` | none by default (or the first-run admin password if set) |
| RabbitMQ management | `http://localhost:15672` | `RABBITMQ_USER` / `RABBITMQ_PASSWORD` (`.env`) |
| Grafana (Loki logs) | `http://localhost:3001` | `admin` / `GRAFANA_PASSWORD` (`.env`) |
| SonarQube (local container) | `http://localhost:9000` | `admin` / `admin` on first login |
| MSSQL | `localhost,1433` | `sa` / `MSSQL_SA_PASSWORD` (`.env`) |
| Redis | `localhost:6379` | `REDIS_PASSWORD` (`.env`) |

Rules for the table:

- Include only services present in the generated `docker-compose.yml` (plus the always-on API/Scalar/health and the web app).
- The credentials column names the **`.env` variable**, never a literal secret.
- All UI rows are the **Development** addresses; note in the surrounding text that the tool dashboards are exposed only when `ASPNETCORE_ENVIRONMENT=Development`.

## SonarQube

Every subproject is Sonar-ready:

- `source/api/.config/dotnet-tools.json` — local tool manifest with `dotnet-sonarscanner` (create with `dotnet new tool-manifest && dotnet tool install dotnet-sonarscanner`).
- `source/api/SonarQube.Analysis.xml` (template) — file-based scanner settings (coverage paths, exclusions). SonarScanner for .NET does not read `sonar-project.properties`; this file is its equivalent and is passed via `/s:` in the script.
- `coverlet.collector` (MIT) in both test projects — produces coverage for Sonar.
- `source/api/coverlet.runsettings` (template) — shared coverage config (OpenCover format, exclusions for `Migrations/` and `Program.cs`). Run coverage locally with `dotnet test --settings coverlet.runsettings`; the OpenCover path matches `sonar.cs.opencover.reportsPaths` in `SonarQube.Analysis.xml`.
- `scripts/sonar-api.sh` (template) runs the full cycle: scanner begin → build → `dotnet test --settings coverlet.runsettings` → scanner end. Requires `SONAR_HOST_URL` and `SONAR_TOKEN` env vars; the project key is `{{solution-name-lower}}-api`.

## Tests

### UnitTests

- xUnit + **NSubstitute** + **FluentAssertions 7.x** (⚠ NOT 8.x — license change; pin the version to `[7.*` in Directory.Packages.props) + **coverlet.collector** (coverage for Sonar).
- Handler tests with substituted repositories, validator tests (`TestValidate`), entity behavior tests.

### IntegrationTests

- **Testcontainers.MsSql** + `WebApplicationFactory<Program>` — a ready-made fixture including the collection definition (the container starts once per run): [../templates/backend/tests/IntegrationTests/ApiFactory.cs](../templates/backend/tests/IntegrationTests/ApiFactory.cs).
- Endpoint tests for the example feature via `HttpClient` (happy path + validation 400).

### ArchitectureTests (mandatory)

Architecture tests are **required in every generated backend and every generation mode** — they keep the Onion dependency rules enforceable over the project's life. Use **NetArchTest.Rules** (MIT) by default, or reflection-based assertions if that fits the template better; pick one approach and stay consistent. They must assert at least:

- `Domain` does not reference `Application`, `Infrastructure` or `Api`.
- `Application` does not reference `Infrastructure` or `Api`.
- `Infrastructure` does not reference `Api`.
- `Api` may reference `Application` and `Infrastructure` (assert the inverse rules above, not this one).
- Endpoints/controllers live only in the `Api` layer.
- `DbContext` lives only in `Infrastructure`.
- Repository implementations live only in `Infrastructure`.
- Business logic (handlers/use cases) does not live in controllers/endpoints.

These run under `dotnet test` alongside the unit and integration tests. The full rationale and the cross-mode rule live in [../.agents/skills/onionforge/SKILL.md](../.agents/skills/onionforge/SKILL.md) (*Architecture tests*).
