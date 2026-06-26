---
name: onionforge
description: Generates a complete, ready-to-implement .NET + React project template from a short business description. Use when the user asks to create/scaffold a new .NET + React project or template ("stwórz szablon projektu", "nowy projekt .net + react", "wygeneruj projekt"). Runs a consultation about tools and libraries (with license warnings), then generates an Onion Architecture backend (MSSQL) and a Vite + React frontend with services, configs and tests.
---

# OnionForge — .NET + React project template generator

Goal: from a short business description, generate a **complete project template** — a .NET backend in Onion Architecture + a React (Vite) frontend — with services, layers, configuration, tests and an example end-to-end flow already written. The code must be ready for implementing the business requirements: the developer gets a working skeleton with patterns to replicate, not an empty solution.

Communicate with the user (questions, summaries) **in the language the user writes in** (typically Polish). Generated code, comments and configs are always in English.

## Generation modes

OnionForge generates at one of three cumulative scope tiers. **Ask the user which mode** at the start of the consultation (Step 3, decision 0) — unless the user has already described the scope explicitly. If the user only says "create a project" with no scope, the default is **Standard**.

### Standard (default)

The complete ready-to-run baseline. Always includes:

- backend (Onion Architecture, MSSQL),
- frontend (Vite + React + TypeScript, connected to the API),
- Docker Compose for the whole project,
- the baseline tests (unit + integration + architecture),
- the `docs/` set and at least one ADR,
- **one end-to-end example feature** derived from the business description,
- the baseline developer tooling (Serilog, OpenAPI, health checks, validation, DI, Sonar config, the chosen commit/versioning tier).

### Extended

Everything in Standard **plus** the optional tools the analysis justifies, for example:

- Redis, RabbitMQ,
- Hangfire or Quartz,
- SignalR,
- authentication and authorization (see *Authentication & authorization variants*),
- local observability (Seq / Grafana+Loki),
- a local SonarQube container (in addition to the always-generated Sonar config),
- additional test coverage.

Add only the tools the description or the user actually calls for — Extended is not "switch everything on".

### Enterprise-ready

Everything in Extended **plus** the elements that prepare the template for team work and deployment:

- a CI/CD skeleton (see *CI/CD skeleton*): backend build, frontend build, tests in the pipeline, Docker image build, optional registry push placeholder, deployment placeholders,
- richer ADRs (one per significant decision, not just the stack),
- versioning and a release workflow (versioning tier 2/3 — MinVer and/or Release Please, per [references/versioning.md](../../../references/versioning.md)).

Higher tiers never remove anything from a lower tier — they only add. When in doubt about whether a tool belongs in Standard or Extended, keep Standard lean and put the tool in Extended.

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
| recurring jobs, task queues, background processing | Hangfire (⚠ open-core) vs Quartz.NET vs BackgroundService — schedules (cron/interval) must be **configurable via appsettings/`.env`**, not hardcoded (see [references/backend.md](../../../references/backend.md), *Scheduled & background jobs*) |
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

**License rule (mandatory — same rule as the head of [references/libraries.md](../../../references/libraries.md); keep the two in sync):**

- [references/libraries.md](../../../references/libraries.md) is the **default source of truth** for cataloged libraries. **Do not re-verify a cataloged library** that is listed there and not in doubt — re-checking every popular package wastes consultation time.
- For libraries **not in the catalog**, check the current license on the web **when the agent has internet access**.
- **Always warn explicitly** (in the option description) when a library is commercial, open-core, dual-license, revenue-dependent, has free-use restrictions, or otherwise needs legal attention — cataloged or not — and offer a free alternative. Known cases: MediatR, AutoMapper, FluentAssertions ≥8, MassTransit v9, Hangfire Pro, QuestPDF.
- If the **user explicitly asks** for a fresh license check, perform it (with internet access).
- If the agent has **no internet access**, state plainly that the recommendation rests on the local catalog and the current upstream license state cannot be confirmed.

**Fixed decisions** — always ask about:

0. **Generation mode** (ask first): Standard / Extended / Enterprise-ready — see *Generation modes*. Skip this question only when the user has already stated the scope; default to Standard if the user just says "create a project". The chosen mode gates which later questions apply (optional tools and auth are Extended+; CI/CD and release workflow are Enterprise-ready).
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
5. **One end-to-end feature** derived from the business description (the most important entity): entity → repository → handler/service → validator → endpoint → frontend page with a TanStack Query hook — with a test at every level. This is the pattern the developer replicates. The feature must be domain-meaningful — see *Example feature quality* (no `Todo`/`WeatherForecast`/`Product`/`Item`/`SampleEntity` defaults).
6. Cross-cutting infrastructure: global exception handler (ProblemDetails), Serilog configuration, **DI registrations per layer including the Api layer** (`AddApi`/`UseApi` in its own `DependencyInjection.cs`; `Program.cs` contains only the composition calls — both are adaptable templates), CORS for the frontend, health check.
7. **Tool UIs exposed in Development:** every chosen tool that ships a web UI (Scalar/Swagger, Hangfire dashboard, RabbitMQ management, Seq, SonarQube...) must be reachable when running in Development, with its URL and credentials source (`.env` variable names) listed in `README.md` and `docs/development.md`. "Reachable" means it actually opens when the api runs in the docker-compose network, not just on `localhost`: a dashboard whose default authorization is local-requests-only (notably the **Hangfire dashboard**) returns 403 from a host browser and must be mapped with an explicit Development authorization filter — use the Hangfire snippet in [templates/MANIFEST.md](../../../templates/MANIFEST.md). Likewise any stateful tool container (Seq, SonarQube) needs a persistent volume so it does not crash on restart.
8. Frontend under `source/web/`: feature-based skeleton, **API client + TanStack Query hooks generated with Orval** from the OpenAPI document emitted by the backend build (`npm run api:generate`, hooked as `predev`; the generated output is committed), configuration of the chosen UI library, example page + a Vitest test.
9. **`docs/` with the initial project documentation, written in English:** `docs/architecture.md` (Onion layers, the chosen stack with licenses, how the pieces talk to each other), `docs/getting-started.md` (prerequisites, first run), `docs/development.md` (ports, tool UIs, `.env` variables, Sonar scans, regenerating the API client) and `docs/adr/0001-technology-stack.md` recording the consultation decisions with their rationale.
10. The template's `README.md`: how to run it (`docker compose up`, or database in Docker + API and frontend locally), a **"Service URLs" table** listing every service/UI in the generated `docker-compose.yml` with its URL and the `.env` variable its credentials come from (canonical catalog and rules in [references/backend.md](../../../references/backend.md) — build it from the project's own compose and `appsettings.json`, one row per chosen tool only), how to run the Sonar scans, how to add the next feature following the pattern, and a pointer to `docs/`.

### Step 6 — Verification

The template must build and pass tests — fix all errors before you finish. **Run the verification once, at the end of generation** (don't re-run full builds and test suites after every intermediate change — it dominates the generation time); after a fix, re-run only the failed step (follow the *Agent Repair Protocol*).

1. `dotnet build` — no errors and no warnings (TreatWarningsAsErrors). The build also emits `source/api/openapi/*.json` — the input for step 3.
2. `dotnet test` — unit and architecture tests always; integration tests (Testcontainers) only when Docker is available — if it is not, note that in the report.
3. Frontend: `npm run api:generate` (after the backend build), then `npm run lint`, `npm run test`, `npm run build`.
4. When Docker is available: `docker compose config`, then `docker compose build` — the compose file must be valid and both Dockerfiles must build successfully.

Then walk the **Final Acceptance Checklist** in full. Any step that cannot run because a tool is missing from the environment is reported as *not verified* with the reason — never as a pass.

### Step 7 — Initialize git

After verification passes, initialize a repository in the project root so the template is ready to push to GitHub/Bitbucket/etc.:

1. `git init -b main` in the project root.
2. Confirm `.gitignore` and `.gitattributes` are present (copied from the templates) so line endings and ignores apply from the first commit — the `.gitattributes` keeps the whole project on one line-ending/encoding style across platforms.
3. **If a commit & versioning tier was chosen** (Step 3, decision 7), set it up now, before committing, per [references/versioning.md](../../../references/versioning.md). Tier 1+ depends on the chosen hook runner: **Husky.Net** → `dotnet new tool-manifest` / `dotnet tool install husky` / `dotnet husky install`, then copy `git/husky/` files; **classic Husky** → copy `git/husky-node/package.json` + configs to the root, `npm install` (its `prepare` runs `husky`), then copy `git/husky-node/` hooks. (Tier 2 MinVer and tier 3 Release Please files are added during generation, not here.)
4. `git add -A` then commit. The required message `first commit` is **not** a Conventional Commit, so when the tier-1 `commit-msg` hook is installed, bypass it for this one scaffold commit: `git commit --no-verify -m "first commit"`. Without hooks, plain `git commit -m "first commit"`. The hook applies from the second commit on.
5. Do **not** add a remote or push — leave that to the developer. Mention in the final report that the repository is initialized with one commit and ready to connect to a remote (`git remote add origin <url> && git push -u origin main`).

If the project directory is already inside an existing git repository (e.g. a monorepo), skip `git init` and just stage and commit the generated files instead.

Finish with a report: what was generated, which stack was chosen, where the developer should start implementing the business requirements, and that the repo is initialized with a `first commit` ready for a remote.

## Generated Project Contract

This is the **minimum output** that must exist after generation. The task is **not done** until every item that applies to the chosen mode is present. Items marked *(Extended+)* / *(Enterprise-ready)* apply only at those tiers; everything else is required in **every** mode including Standard. If something cannot be produced, say so explicitly in the report — never silently skip a contract item.

### Required root files

- `README.md` (with the Service URLs table)
- `docker-compose.yml`
- `.env.example`
- `.editorconfig`
- `.gitattributes`
- `.gitignore`
- `docs/architecture.md`
- `docs/getting-started.md`
- `docs/development.md`
- at least one ADR, e.g. `docs/adr/0001-technology-stack.md`

### Required backend

- a solution in the backend directory (`source/api/`)
- projects following Onion / Clean Architecture: **Domain**, **Application**, **Infrastructure**, **Api/Web**
- central package management (`Directory.Packages.props`) — the repo standard, always
- OpenAPI configuration (and the build-time OpenAPI document for Orval)
- health checks (`/health`)
- logging (Serilog: Console + the chosen sink)
- validation (FluentValidation)
- dependency injection wired per layer (including the Api layer)
- **one end-to-end example feature** (see *Example feature quality*)
- tests: **Unit tests**, **Integration tests**, **Architecture tests** (see *Architecture tests*)

### Required frontend

- Vite + React + TypeScript
- routing
- TanStack Query
- Orval (or another agreed API-client generator)
- the **generated API client**, committed
- one page for the example feature, with loading / error / success handling
- at least one frontend test (Vitest)

### Required infrastructure

- Docker Compose
- `.env.example` documenting every variable
- every chosen developer service reachable from the host
- **no hardcoded secrets** (only explicit developer placeholders)
- in-Docker addresses use **compose service names**, not `localhost`
- documented ports and service addresses (the README Service URLs table)

### Required only at higher tiers

- *(Extended+)* the optional tools chosen during consultation, each actually wired and reachable
- *(Extended+)* authentication/authorization wired end-to-end **if** auth was chosen (see *Authentication & authorization variants*)
- *(Enterprise-ready)* a CI/CD skeleton (see *CI/CD skeleton*)
- *(Enterprise-ready)* versioning + release workflow and richer ADRs

## Final Acceptance Checklist

After generation, the agent **must** walk this checklist. **If a step cannot run because a tool is missing from the environment, the agent must not pretend it passed** — it must list the step as "not verified" and say why. Faking success is a hard failure of this skill.

- [ ] `docker compose config` passes.
- [ ] `docker compose build` passes **if Docker is available**.
- [ ] Backend builds locally (`dotnet build`, no errors/warnings).
- [ ] Backend tests pass — or the agent clearly reports which it could not run and why (e.g. integration tests need Docker).
- [ ] The API starts.
- [ ] `/health` responds.
- [ ] OpenAPI JSON is generated or available at a documented endpoint.
- [ ] Frontend installs dependencies (`npm install`).
- [ ] Frontend builds (`npm run build`).
- [ ] API client generation works (e.g. `npm run api:generate` when Orval is used).
- [ ] The generated API client is present in the expected directory.
- [ ] `README.md` contains a correct Service URLs table.
- [ ] `.env.example` contains every required variable.
- [ ] No hardcoded credentials beyond explicit developer placeholders.
- [ ] The example feature works end-to-end across backend and frontend code.
- [ ] *(Extended+ with auth)* the auth flow is consistent across backend, frontend and docs.
- [ ] *(Enterprise-ready)* the CI/CD skeleton is present and internally consistent.
- [ ] The agent explicitly lists everything it could **not** verify.

**Rule:** a step that could not be executed due to a missing tool in the environment is reported as *not verified*, with the reason. The agent never reports such a step as successful.

## Agent Repair Protocol

When a verification step fails, repair it like this:

1. **Read the exact error** — do not guess from the symptom.
2. **Fix the smallest area** tied to that error.
3. **Do not rebuild the whole architecture** because of one error.
4. **Do not delete features or tools** just to make a build pass.
5. **Re-run only the failed step** (or the step directly downstream of the fix), not the entire verification suite.
6. **Report what was fixed.**
7. **Report what could not be fixed.**
8. **Never hide errors.**
9. **Do not change previously approved technology decisions** without an explicit, stated reason (and, where it matters, the user's confirmation).

The goal of repair is to make the approved template build and pass — not to shrink the template until it trivially passes.

## Architecture tests

Architecture tests are **mandatory** for every generated backend, in every mode. They guard the Onion dependency rules so the developer cannot silently break the architecture later. Put them in a dedicated test project (`source/api/tests/{Name}.ArchitectureTests`) using **NetArchTest.Rules** (MIT) — or reflection-based tests if that fits the rest of the template better; pick one approach and keep it consistent.

They must assert at least:

- **Domain** does not reference Application, Infrastructure or Api/Web.
- **Application** does not reference Infrastructure or Api/Web.
- **Infrastructure** does not reference Api/Web.
- **Api/Web** may reference Application and Infrastructure (allowed — assert the inverse rules above, not this one).
- Endpoints/controllers live only in the **Api/Web** layer.
- `DbContext` lives only in **Infrastructure**.
- Repository implementations live only in **Infrastructure**.
- Business logic does not live in controllers/endpoints (e.g. handlers/use cases are not defined in the Api layer).

These tests run as part of `dotnet test` and count toward the backend test results in the Final Acceptance Checklist.

## Authentication & authorization variants

When auth is in scope (Extended+), pick **one** variant and implement it **end-to-end** — backend, frontend and docs must agree. **Do not generate a half-built auth flow:** if auth is chosen, the minimal flow must be coherent everywhere.

### None

No auth. Suitable for simple projects, demos or public APIs. Nothing auth-related is generated.

### ASP.NET Core Identity + JWT

For apps with local user accounts. Required output:

- Identity configuration,
- JWT configuration,
- register / login / refresh endpoints (those that fit the scope),
- a protected endpoint example,
- frontend auth state,
- a protected route example on the frontend,
- documentation of the required environment variables.

### External Identity Provider (placeholder)

For apps where auth is delegated to an external IdP (Keycloak, Auth0, Entra ID, OpenIddict, etc.). Required output:

- placeholder configuration for the IdP,
- a description of the required environment variables,
- a protected endpoint example,
- a protected frontend route example,
- integration documentation.

Warn about license/cost where relevant (e.g. ⚠ Duende IdentityServer — see [references/libraries.md](../../../references/libraries.md)).

## Example feature quality

The end-to-end example feature **must derive from the user's business description** — it is the pattern the developer replicates, so it has to look like real domain code.

**Forbidden as default examples** (unless the user actually asks for them): `Todo`, `WeatherForecast`, `Product`, `Item`, `SampleEntity`. The feature must be domain-meaningful. Examples of good picks:

- reservation app → reservation request / booking slot,
- finance app → expense report / transaction category,
- education app → flashcard deck / learning session,
- RPG app → spell / character spellbook,
- e-commerce app → order draft / cart checkout step.

The feature must include at minimum:

- a domain entity or aggregate,
- a command/query or service use case,
- validation,
- persistence,
- an API endpoint,
- a unit test,
- an integration or functional test,
- the generated frontend client,
- a frontend page that uses the API,
- loading / error / success handling on the frontend.

## CI/CD skeleton

Generated only for **Enterprise-ready** mode (skip it in Standard/Extended). Produce a pipeline **skeleton** — e.g. GitHub Actions under `.github/workflows/` — that contains:

- restore / install dependencies,
- build backend,
- run backend tests,
- install frontend dependencies,
- build frontend,
- run frontend tests (if configured),
- docker build,
- an **optional registry push as a clearly marked placeholder**,
- readable `TODO` markers for deployment.

**Do not implement a full deployment to a specific cloud** unless the user explicitly chose one. Leave a safe, readable placeholder instead of a half-working deploy. Keep secrets out of the workflow file — reference repository/organization secrets by name only.
