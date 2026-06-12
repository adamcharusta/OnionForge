# Template manifest

**Copy the files in this directory, do not generate them from scratch.** After copying, replace the placeholders and change nothing else (unless the consultation decisions require it — then use the snippets at the end of this file).

Files that start with a dot in the target project are stored here **without the dot** (e.g. `gitignore` → `.gitignore`, `dockerignore` → `.dockerignore`) — add the dot when copying.

## Generated project layout

```
{project-name}/
├── .gitignore                 # general: OS + IDE + secrets (from root/gitignore)
├── .editorconfig
├── .env / .env.example
├── docker-compose.yml         # runs the whole project: mssql + api + web (+ tools)
├── scripts/                   # bash scripts (Sonar scans, helpers)
└── source/
    ├── api/                   # .NET solution (own .gitignore, .dockerignore, Dockerfile, Sonar)
    └── web/                   # Vite + React (own .gitignore, .dockerignore, Dockerfile, Sonar)
```

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

### Frontend (`source/web/`)

| Template | Target | Placeholders |
|---|---|---|
| `web/gitignore` | `source/web/.gitignore` | — |
| `web/dockerignore` | `source/web/.dockerignore` | — |
| `web/Dockerfile` | `source/web/Dockerfile` | — |
| `web/nginx.conf` | `source/web/nginx.conf` | — |
| `web/sonar-project.properties` | `source/web/sonar-project.properties` | solution-name-lower, SolutionName |
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
  "dev": "vite",
  "build": "tsc -b && vite build",
  "lint": "eslint .",
  "format": "prettier --write .",
  "test": "vitest run",
  "test:watch": "vitest",
  "coverage": "vitest run --coverage"
}
```
