# OnionForge

**OnionForge** is a tooling repository. Its product is the **`onionforge` agent skill**, which
turns a short business description into a complete, ready-to-implement project template:

- a **.NET backend** in Onion Architecture (Domain / Application / Infrastructure / Api) on **MSSQL**, with
  FluentValidation, Serilog, OpenAPI, a global exception handler, health checks and tests
  (xUnit + NSubstitute + FluentAssertions 7.x + Testcontainers);
- a **Vite + React + TypeScript frontend** with TanStack Query, an Orval-generated API client,
  ESLint (flat config) + Prettier and Vitest;
- the surrounding glue: `docker-compose.yml` for the whole stack, SonarQube configuration,
  `.env`-driven secrets, `docs/`, and an optional Conventional-Commits / versioning workflow.

The skill does not generate an empty solution — it writes **one end-to-end feature** derived from the
description (entity → repository → handler → validator → endpoint → frontend page, with a test at every
level) as the pattern the developer replicates.

## How it works

The skill runs a short **consultation** before generating: it analyses the description, then asks about
the stack (data access, mediator, mapper, logging sink, UI library, background jobs, auth, versioning…)
as option lists — each with a recommendation and a **license warning** where relevant (the .NET ecosystem
has several open-core / commercial-since-vN libraries). After you approve the summary it generates the
project, then **builds and tests it** before finishing.

```
business description
  → analysis (entities + technical needs)
  → tooling consultation (with license verification)
  → summary & approval
  → generation (templates first, then the variable code)
  → build + test verification
  → git init + first commit
```

## Using it

Open this repository in an agent that supports the [Agent Skills standard](https://agents.md) and ask, e.g.:

> *"stwórz szablon projektu .NET + React dla ..."* / *"scaffold a new .NET + React project for ..."*

- **Claude Code** — the skill is exposed via [.claude/skills/onionforge/](.claude/skills/onionforge/); invoke it or just describe the project.
- **Codex / GitHub Copilot / other AGENTS.md-aware tools** — the skill is discovered automatically from [.agents/skills/onionforge/](.agents/skills/onionforge/).

## Repository layout

| Path | What it is |
|---|---|
| [.agents/skills/onionforge/SKILL.md](.agents/skills/onionforge/SKILL.md) | **Canonical skill definition** (the workflow). |
| [.claude/skills/onionforge/SKILL.md](.claude/skills/onionforge/SKILL.md) | Thin Claude Code adapter pointing at the canonical file. |
| [references/](references/) | Generation conventions: `backend.md`, `frontend.md`, `libraries.md` (license catalog), `versioning.md`. |
| [templates/](templates/) | Invariant files copied verbatim into generated projects (placeholder substitution only). Start at [templates/MANIFEST.md](templates/MANIFEST.md). |
| [AGENTS.md](AGENTS.md) / [CLAUDE.md](CLAUDE.md) | Agent-facing rules for working **in this repo**. `CLAUDE.md` imports `AGENTS.md`. |

## Contributing

- All documentation in this repo is written in **English**; the skill itself converses with the user in
  their own language (typically Polish). Generated code and configs are always English.
- Change the workflow only in the **canonical** [.agents/skills/onionforge/SKILL.md](.agents/skills/onionforge/SKILL.md) — the `.claude/` adapter stays a thin pointer.
- Files in [templates/](templates/) are copied verbatim into generated projects — keep them buildable and
  tool-agnostic, and follow the encoding rule in the manifest (UTF-8 without BOM, ASCII-only comments).
- Generated projects are **test artifacts**: when something is wrong in the output, fix the skill
  (references / templates), not the generated project.

See [AGENTS.md](AGENTS.md) for the full set of rules.
