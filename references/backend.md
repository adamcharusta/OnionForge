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
    │   ├── {Name}.sln
    │   ├── Directory.Build.props          # shared settings for all projects
    │   ├── Directory.Packages.props       # central package management
    │   ├── .gitignore / .dockerignore / Dockerfile
    │   ├── .config/dotnet-tools.json      # dotnet-sonarscanner local tool
    │   ├── SonarQube.Analysis.xml         # scanner settings (the .NET counterpart of sonar-project.properties)
    │   ├── openapi/                       # OpenAPI document emitted by the Api build (input for Orval)
    │   ├── src/
    │   │   ├── {Name}.Domain/             # entities, value objects, enums, domain exceptions
    │   │   ├── {Name}.Application/        # use cases, DTOs, validators, interfaces (IRepository, I*Service)
    │   │   ├── {Name}.Infrastructure/     # implementations: data access, integrations, clock, e-mail etc.
    │   │   └── {Name}.Api/                # endpoints, middleware, DI composition, appsettings
    │   └── tests/
    │       ├── {Name}.UnitTests/          # xUnit + NSubstitute + FluentAssertions 7.x
    │       └── {Name}.IntegrationTests/   # Testcontainers.MsSql + WebApplicationFactory
    └── web/                               # frontend — see frontend.md
```

Dependency rules (enforced via project references):

- `Domain` → nothing (zero NuGet packages besides analyzers),
- `Application` → `Domain`,
- `Infrastructure` → `Application`,
- `Api` → `Application` + `Infrastructure` (for DI registration only).

## Invariant files — copy from templates

`Directory.Build.props`, `.editorconfig`, root and api `.gitignore`, `.dockerignore`, `Dockerfile`, `docker-compose.yml`, `.env.example`, `appsettings.json`, `SonarQube.Analysis.xml`, the Sonar scripts, `Entity`, `DomainException`, `IDateTimeProvider`/`DateTimeProvider`, `GlobalExceptionHandler`, `ApiFactory` and the custom mediator files are ready in [../templates/](../templates/). Copy them according to [../templates/MANIFEST.md](../templates/MANIFEST.md) (placeholders and variant snippets are documented there) — do not write them from scratch. `Api/Program.cs` and `Api/DependencyInjection.cs` are **adaptable** templates: copy them and add stack-specific code only at the `// EXTEND:` markers.

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
- **Every tool with a web UI** (Scalar/Swagger, Hangfire dashboard, etc.) must be reachable when running in Development and listed with its URL in the README and `docs/development.md`.
- CORS policy for the frontend origin (from configuration).
- Health check: `/health` (+ a database check).
- `appsettings.json` + `appsettings.Development.json` with the MSSQL connection string matching docker-compose.

## Docker

- Root `docker-compose.yml` (template: `../templates/root/docker-compose.yml`) runs the **whole project**: MSSQL with a healthcheck, the API (built from `source/api/Dockerfile`) and the web frontend (built from `source/web/Dockerfile`). If RabbitMQ, Redis, Seq or a local SonarQube was chosen — append the services from the snippets in MANIFEST.md.
- **All credentials and the runtime environment come from `.env`** (gitignored, with a provided `.env.example`): the SA password, credentials of every added service (RabbitMQ user/password, Redis password...) and `ASPNETCORE_ENVIRONMENT` of the api container (`${ASPNETCORE_ENVIRONMENT:-Development}` — switchable without editing the compose file). Never hardcode defaults like `guest/guest`.
- `source/api/Dockerfile` is a multi-stage build (SDK → aspnet runtime, port 8080); `source/api/.dockerignore` keeps build context lean. Both are templates — copy, do not write.

## SonarQube

Every subproject is Sonar-ready:

- `source/api/.config/dotnet-tools.json` — local tool manifest with `dotnet-sonarscanner` (create with `dotnet new tool-manifest && dotnet tool install dotnet-sonarscanner`).
- `source/api/SonarQube.Analysis.xml` (template) — file-based scanner settings (coverage paths, exclusions). SonarScanner for .NET does not read `sonar-project.properties`; this file is its equivalent and is passed via `/s:` in the script.
- `coverlet.collector` (MIT) in both test projects — produces opencover coverage for Sonar.
- `scripts/sonar-api.sh` (template) runs the full cycle: scanner begin → build → tests with coverage → scanner end. Requires `SONAR_HOST_URL` and `SONAR_TOKEN` env vars; the project key is `{{solution-name-lower}}-api`.

## Tests

### UnitTests

- xUnit + **NSubstitute** + **FluentAssertions 7.x** (⚠ NOT 8.x — license change; pin the version to `[7.*` in Directory.Packages.props) + **coverlet.collector** (coverage for Sonar).
- Handler tests with substituted repositories, validator tests (`TestValidate`), entity behavior tests.

### IntegrationTests

- **Testcontainers.MsSql** + `WebApplicationFactory<Program>` — a ready-made fixture including the collection definition (the container starts once per run): [../templates/backend/tests/IntegrationTests/ApiFactory.cs](../templates/backend/tests/IntegrationTests/ApiFactory.cs).
- Endpoint tests for the example feature via `HttpClient` (happy path + validation 400).
