---
name: onionforge
description: Generates a complete, ready-to-implement .NET + React project template from a short business description. Use when the user asks to create/scaffold a new .NET + React project or template ("stwórz szablon projektu", "nowy projekt .net + react", "wygeneruj projekt"). Runs a consultation about tools and libraries (with license warnings), then generates an Onion Architecture backend (MSSQL) and a Vite + React frontend with services, configs and tests.
---

# OnionForge — .NET + React project template generator

Goal: from a short business description, generate a **complete project template** — a .NET backend in Onion Architecture + a React (Vite) frontend — with services, layers, configuration, tests and an example end-to-end flow already written. The code must be ready for implementing the business requirements: the developer gets a working skeleton with patterns to replicate, not an empty solution.

Communicate with the user (questions, summaries) **in the language the user writes in** (typically Polish). Generated code, comments and configs are always in English.

## Workflow

### Step 1 — Business description

If the user has not provided a business description, ask for one (a few sentences: what the system does, who uses it, the key processes). Do not start generating without it.

Also establish (if not clear from the conversation): the project/solution name and the target directory.

### Step 2 — Analyze the description

Derive from the description:

- **domain entities** and the relations between them (used to generate the example feature),
- **technical needs** — every detected need becomes a dynamic question in step 3:

| Signal in the description | Proposal to consult |
|---|---|
| recurring jobs, task queues, background processing | Hangfire (⚠ open-core) vs Quartz.NET vs BackgroundService |
| live notifications, chat, real-time dashboard | SignalR |
| integrations with external APIs | Refit + Polly |
| user sign-in, roles | ASP.NET Core Identity vs external IdP (Keycloak/Auth0/Entra ID) |
| heavy read traffic, caching | IMemoryCache vs Redis (StackExchange.Redis) |
| service-to-service communication, events | Wolverine messaging vs MassTransit (⚠ v9 commercial) vs RabbitMQ.Client |
| files, documents, images | local storage vs Azure Blob/S3 |
| reports, exports | ClosedXML, QuestPDF (⚠ revenue-dependent license) |

Full catalog with licenses: [references/libraries.md](../../../references/libraries.md).

### Step 3 — Tooling consultation

Ask questions as option lists with a recommendation and a short justification. If the agent has a native tool for option-based questions (e.g. AskUserQuestion in Claude Code), use it; in other agents (Codex, Copilot) ask the questions in a regular reply and wait for the user's choices.

**License rule (mandatory):** before recommending any library, check its current license (NuGet/GitHub; search the web when in doubt, if you have network access). If a library is commercial or dual-license — **warn explicitly in the option description** and offer a free alternative. Known cases: MediatR, AutoMapper, FluentAssertions ≥8, MassTransit v9, Hangfire Pro, QuestPDF. Details in [references/libraries.md](../../../references/libraries.md).

**Fixed decisions** — always ask about:

1. **Data access** (the database is **always MSSQL** — do NOT ask about the database engine):
   - EF Core (ORM, code-based migrations),
   - Dapper (micro-ORM, full control over SQL),
   - Dapper + SQL migration scripts (DbUp or grate).
2. **Mediator / command and query handling:**
   - Wolverine,
   - MediatR (⚠ commercial license since v13),
   - own lightweight implementation (`ICommandHandler<,>` / `IQueryHandler<,>` + DI registration — pattern in [references/backend.md](../../../references/backend.md)).
3. **Mapper:**
   - Mapperly (source generator, MIT),
   - Mapster (MIT),
   - AutoMapper (⚠ commercial license since v15),
   - manual mapping (static methods on DTOs).
4. **Second Serilog sink** (Console is always on; pick the second one):
   - Seq, rolling file, MSSQL, Grafana Loki.
5. **Frontend UI:** Tailwind CSS vs MUI.
6. **SonarQube:** existing instance / SonarCloud (only env vars needed) vs a local `sonarqube:community` container added to docker-compose (⚠ resource-heavy). Sonar configuration itself is always generated for both subprojects — this question is only about where the server runs.

**Dynamic decisions** — questions derived from the step 2 analysis (backend and frontend), each proposal with a short business justification and license info. For the frontend, analyze which libraries fit the domain (forms → react-hook-form + zod; tables → TanStack Table; charts → Recharts; routing → React Router; etc. — catalog in libraries.md) and ask about them.

**Fixed elements — do NOT ask about these, they are always in the template:**

- Backend: Onion Architecture, MSSQL, FluentValidation, Serilog (Console + the chosen sink), OpenAPI, tests with xUnit + NSubstitute + FluentAssertions **7.x** + Testcontainers (MsSql).
- Frontend: Vite + TypeScript, Vitest, TanStack Query, ESLint (flat config) + Prettier with the full plugin set per [references/frontend.md](../../../references/frontend.md).

### Step 4 — Summary and approval

Present a concise summary: the chosen stack (with licenses), the solution structure, the entity list and the example feature that will be implemented end-to-end. Wait for approval before generating.

### Step 5 — Generation

Generate according to the conventions in the reference files — read them before starting:

- [references/backend.md](../../../references/backend.md) — solution structure, code patterns, DI, tests,
- [references/frontend.md](../../../references/frontend.md) — structure, configs (ESLint/Prettier/Vitest), TanStack Query.

**Templates first, generation second:** the invariant files (configs, cross-cutting classes, test fixtures) are ready in [templates/](../../../templates/) — copy them according to [templates/MANIFEST.md](../../../templates/MANIFEST.md) and replace only the placeholders (`{{SolutionName}}`, `{{TargetFramework}}`, `{{ApiPort}}`...). **Do not write these files from scratch and do not modify their content** beyond the placeholders and the variant snippets from the manifest. You generate from scratch only the variable code: domain entities, the end-to-end feature, DI registrations, frontend pages.

Mandatory scope:

1. **Fixed project layout:** root with a general `.gitignore` (OS + IDE + secrets), `.editorconfig`, `.env.example`, a `docker-compose.yml` that runs the whole project (MSSQL + api + web + chosen tools), `scripts/` with bash scripts, and `source/` with the subprojects: `api/` (.NET), `web/` (frontend) and any other subproject the analysis justifies (e.g. a worker service).
2. **Each subproject is self-contained:** its own technology-specific `.gitignore`, `.dockerignore` and `Dockerfile`, plus SonarQube configuration (api: dotnet-sonarscanner tool manifest + coverlet; web: `sonar-project.properties` + lcov coverage) with run scripts in `scripts/`.
3. Backend solution under `source/api/`: layer projects (`Domain`, `Application`, `Infrastructure`, `Api`) + test projects, `Directory.Build.props`, `Directory.Packages.props`.
4. **One end-to-end feature** derived from the business description (the most important entity): entity → repository → handler/service → validator → endpoint → frontend page with a TanStack Query hook — with a test at every level. This is the pattern the developer replicates.
5. Cross-cutting infrastructure: global exception handler (ProblemDetails), Serilog configuration, DI registrations per layer, CORS for the frontend, health check.
6. Frontend under `source/web/`: feature-based skeleton, API client, configuration of the chosen UI library, example page + a Vitest test.
7. The template's `README.md`: how to run it (`docker compose up`, or database in Docker + API and frontend locally), how to run the Sonar scans, how to add the next feature following the pattern.

### Step 6 — Verification

The template must build and pass tests — fix all errors before you finish:

1. `dotnet build` — no errors and no warnings (TreatWarningsAsErrors).
2. `dotnet test` — unit tests always; integration tests (Testcontainers) only when Docker is available — if it is not, note that in the report.
3. Frontend: `npm run lint`, `npm run test`, `npm run build`.
4. When Docker is available: `docker compose build` — both Dockerfiles must build successfully.

Finish with a report: what was generated, which stack was chosen, and where the developer should start implementing the business requirements.
