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

Grafana Loki (package `Serilog.Sinks.Grafana.Loki`):

```json
{ "Name": "GrafanaLoki", "Args": { "uri": "http://localhost:3100" } }
```

### Services with credentials — append to `docker-compose.yml` when chosen

Whenever one of these is added, also add its variables to `.env` **and** `.env.example`, and use the same variables in the api connection strings (never hardcode `guest/guest` or empty passwords).

RabbitMQ (variables: `RABBITMQ_USER`, `RABBITMQ_PASSWORD`; api connection string: `amqp://${RABBITMQ_USER}:${RABBITMQ_PASSWORD}@rabbitmq:5672`; management UI on http://localhost:15672):

```yaml
  rabbitmq:
    image: rabbitmq:4-management-alpine
    container_name: {{solution-name-lower}}-rabbitmq
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
```

Redis (variable: `REDIS_PASSWORD`; api connection string: `redis:6379,password=${REDIS_PASSWORD}`):

```yaml
  redis:
    image: redis:7-alpine
    container_name: {{solution-name-lower}}-redis
    command: ["redis-server", "--requirepass", "${REDIS_PASSWORD}"]
    ports:
      - "6379:6379"
```

### Seq service — append to `docker-compose.yml` when Seq was chosen

```yaml
  seq:
    image: datalust/seq:latest
    container_name: {{solution-name-lower}}-seq
    environment:
      ACCEPT_EULA: "Y"
    ports:
      - "5341:80"
```

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
