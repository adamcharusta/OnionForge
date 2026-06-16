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

**License rule (mandatory):** before recommending any library, check its license against [references/libraries.md](../../../references/libraries.md) — that catalog is the source of truth; **search the web only for libraries not listed there or when genuinely in doubt** (do not re-verify the cataloged ones — it slows the consultation down). If a library is commercial or dual-license — **warn explicitly in the option description** and offer a free alternative. Known cases: MediatR, AutoMapper, FluentAssertions ≥8, MassTransit v9, Hangfire Pro, QuestPDF.

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
7. **Commit & versioning workflow** (cumulative tiers; **default tier 1** — present all and recommend tier 1):
   - **None** — plain git (the project still gets `.gitattributes` and the `first commit`).
   - **Tier 1 — Conventional Commits + git hooks** (recommended): `commit-msg` lint + `pre-commit` format, local, cross-platform, no CI coupling.
   - **Tier 2 — + MinVer** tag-based SemVer for .NET and the web app version from the same git tag.
   - **Tier 3 — + Release Please** GitHub Actions automation (auto bump, CHANGELOG, tag, release). ⚠ couples the repo to GitHub Actions.

   **When tier 1+ is chosen, also ask the hook runner:**
   - **Husky.Net** (recommended) — a .NET tool; no extra Node, no root `package.json`; commit linting via a bundled `.csx` script.
   - **Classic Husky (Node)** — the npm ecosystem (husky + commitlint + lint-staged); ⚠ adds a small root `package.json`/`node_modules` because git hooks live at the repo root while Node otherwise lives only in `source/web`.

   Both enforce the same Conventional Commits rules; pick by which ecosystem the team prefers. All tools are free / permissive (Conventional Commits, Husky.Net, Husky, commitlint, MinVer, Release Please). Full setup, files and snippets per tier and per hook runner: [references/versioning.md](../../../references/versioning.md).

**Dynamic decisions** — questions derived from the step 2 analysis (backend and frontend), each proposal with a short business justification and license info. For the frontend, analyze which libraries fit the domain (forms → react-hook-form + zod; tables → TanStack Table; charts → Recharts; routing → React Router; etc. — catalog in libraries.md) and ask about them.

**Fixed elements — do NOT ask about these, they are always in the template:**

- Backend: Onion Architecture, MSSQL, FluentValidation, Serilog (Console + the chosen sink), OpenAPI, tests with xUnit + NSubstitute + FluentAssertions **7.x** + Testcontainers (MsSql).
- Frontend: Vite + TypeScript, Vitest, TanStack Query, ESLint (flat config) + Prettier with the full plugin set per [references/frontend.md](../../../references/frontend.md).

### Step 4 — Summary and approval

Present a concise summary: the chosen stack (with licenses), the solution structure, the entity list and the example feature that will be implemented end-to-end. Wait for approval before generating.

### Step 5 — Generation

Generate according to the conventions in the reference files — read them before starting:

- [references/backend.md](../../../references/backend.md) — solution structure, code patterns, DI, tests,
- [references/frontend.md](../../../references/frontend.md) — structure, configs (ESLint/Prettier/Vitest), TanStack Query,
- [references/versioning.md](../../../references/versioning.md) — commit & versioning workflow tiers (only when tier 1+ was chosen).

**Templates first, generation second:** the invariant files (configs, cross-cutting classes, test fixtures) are ready in [templates/](../../../templates/) — copy them according to [templates/MANIFEST.md](../../../templates/MANIFEST.md) and replace only the placeholders (`{{SolutionName}}`, `{{TargetFramework}}`, `{{ApiPort}}`...). **Do not write these files from scratch and do not modify their content** beyond the placeholders and the variant snippets from the manifest; files marked **adaptable** in the manifest (`Api/Program.cs`, `Api/DependencyInjection.cs`) may additionally be extended, but only at their `// EXTEND:` markers. You generate from scratch only the variable code: domain entities, the end-to-end feature, layer DI registrations, frontend pages, `docs/` content.

**Encoding:** write every file as UTF-8 without BOM and keep code/config comments ASCII-only (no em-dashes or smart quotes in `.cs`/`.ts`/`.xml`/config) — see the encoding rule in the manifest. This prevents Windows mojibake.

Mandatory scope:

1. **Fixed project layout:** root with a general `.gitignore` (OS + IDE + secrets), `.editorconfig`, `.env.example`, a `docker-compose.yml` that runs the whole project (MSSQL + api + web + chosen tools), `docs/` with project documentation, `scripts/` with bash scripts, and `source/` with the subprojects: `api/` (.NET), `web/` (frontend) and any other subproject the analysis justifies (e.g. a worker service).
2. **Each subproject is self-contained:** its own technology-specific `.gitignore`, `.dockerignore` and `Dockerfile`, plus SonarQube configuration (api: dotnet-sonarscanner tool manifest + `SonarQube.Analysis.xml` + coverlet; web: `sonar-project.properties` + lcov coverage) with run scripts in `scripts/`.
3. **docker-compose is fully `.env`-driven:** credentials of every service that supports them (MSSQL, RabbitMQ, Redis...) come from `.env` variables — never hardcoded defaults like `guest/guest` — and `ASPNETCORE_ENVIRONMENT` of the api container is switchable via `.env` (default `Development`). `.env.example` documents every variable.
4. Backend solution under `source/api/`: layer projects (`Domain`, `Application`, `Infrastructure`, `Api`) + test projects, `Directory.Build.props`, `Directory.Packages.props`.
5. **One end-to-end feature** derived from the business description (the most important entity): entity → repository → handler/service → validator → endpoint → frontend page with a TanStack Query hook — with a test at every level. This is the pattern the developer replicates.
6. Cross-cutting infrastructure: global exception handler (ProblemDetails), Serilog configuration, **DI registrations per layer including the Api layer** (`AddApi`/`UseApi` in its own `DependencyInjection.cs`; `Program.cs` contains only the composition calls — both are adaptable templates), CORS for the frontend, health check.
7. **Tool UIs exposed in Development:** every chosen tool that ships a web UI (Scalar/Swagger, Hangfire dashboard, RabbitMQ management, Seq, SonarQube...) must be reachable when running in Development, with its URL and credentials source (`.env` variable names) listed in `README.md` and `docs/development.md`. "Reachable" means it actually opens when the api runs in the docker-compose network, not just on `localhost`: a dashboard whose default authorization is local-requests-only (notably the **Hangfire dashboard**) returns 403 from a host browser and must be mapped with an explicit Development authorization filter — use the Hangfire snippet in [templates/MANIFEST.md](../../../templates/MANIFEST.md). Likewise any stateful tool container (Seq, SonarQube) needs a persistent volume so it does not crash on restart.
8. Frontend under `source/web/`: feature-based skeleton, **API client + TanStack Query hooks generated with Orval** from the OpenAPI document emitted by the backend build (`npm run api:generate`, hooked as `predev`; the generated output is committed), configuration of the chosen UI library, example page + a Vitest test.
9. **`docs/` with the initial project documentation, written in English:** `docs/architecture.md` (Onion layers, the chosen stack with licenses, how the pieces talk to each other), `docs/getting-started.md` (prerequisites, first run), `docs/development.md` (ports, tool UIs, `.env` variables, Sonar scans, regenerating the API client) and `docs/adr/0001-technology-stack.md` recording the consultation decisions with their rationale.
10. The template's `README.md`: how to run it (`docker compose up`, or database in Docker + API and frontend locally), a **"Service URLs" table** listing every service/UI in the generated `docker-compose.yml` with its URL and the `.env` variable its credentials come from (canonical catalog and rules in [references/backend.md](../../../references/backend.md) — build it from the project's own compose and `appsettings.json`, one row per chosen tool only), how to run the Sonar scans, how to add the next feature following the pattern, and a pointer to `docs/`.

### Step 6 — Verification

The template must build and pass tests — fix all errors before you finish. **Run the verification once, at the end of generation** (don't re-run full builds and test suites after every intermediate change — it dominates the generation time); after a fix, re-run only the failed step.

1. `dotnet build` — no errors and no warnings (TreatWarningsAsErrors). The build also emits `source/api/openapi/*.json` — the input for step 3.
2. `dotnet test` — unit tests always; integration tests (Testcontainers) only when Docker is available — if it is not, note that in the report.
3. Frontend: `npm run api:generate` (after the backend build), then `npm run lint`, `npm run test`, `npm run build`.
4. When Docker is available: `docker compose build` — both Dockerfiles must build successfully.

### Step 7 — Initialize git

After verification passes, initialize a repository in the project root so the template is ready to push to GitHub/Bitbucket/etc.:

1. `git init -b main` in the project root.
2. Confirm `.gitignore` and `.gitattributes` are present (copied from the templates) so line endings and ignores apply from the first commit — the `.gitattributes` keeps the whole project on one line-ending/encoding style across platforms.
3. **If a commit & versioning tier was chosen** (Step 3, decision 7), set it up now, before committing, per [references/versioning.md](../../../references/versioning.md). Tier 1+ depends on the chosen hook runner: **Husky.Net** → `dotnet new tool-manifest` / `dotnet tool install husky` / `dotnet husky install`, then copy `git/husky/` files; **classic Husky** → copy `git/husky-node/package.json` + configs to the root, `npm install` (its `prepare` runs `husky`), then copy `git/husky-node/` hooks. (Tier 2 MinVer and tier 3 Release Please files are added during generation, not here.)
4. `git add -A` then commit. The required message `first commit` is **not** a Conventional Commit, so when the tier-1 `commit-msg` hook is installed, bypass it for this one scaffold commit: `git commit --no-verify -m "first commit"`. Without hooks, plain `git commit -m "first commit"`. The hook applies from the second commit on.
5. Do **not** add a remote or push — leave that to the developer. Mention in the final report that the repository is initialized with one commit and ready to connect to a remote (`git remote add origin <url> && git push -u origin main`).

If the project directory is already inside an existing git repository (e.g. a monorepo), skip `git init` and just stage and commit the generated files instead.

Finish with a report: what was generated, which stack was chosen, where the developer should start implementing the business requirements, and that the repo is initialized with a `first commit` ready for a remote.
