# OnionForge

Tooling repository: the **onionforge** skill generates ready-to-implement .NET + React project templates (Onion Architecture backend + MSSQL, Vite + React + TypeScript frontend) from a short business description, after consulting the library choices with the user.

## Skill

Canonical definition: [.agents/skills/onionforge/SKILL.md](.agents/skills/onionforge/SKILL.md) (Agent Skills standard — discovered automatically by Codex, GitHub Copilot and other compliant agents; `.claude/skills/onionforge/` is an adapter for Claude Code). The reference docs ([references/](references/)) and file templates ([templates/](templates/)) it uses live at the repository root.

When the user asks for a .NET + React template/project, follow that definition exactly: business description → analysis → tooling consultation (with license verification) → approval → generation → build and test verification.

## Rules for editing this repo

- All skill documentation is written in **English**. The skill communicates with the user in the language the user writes in (typically Polish); generated code and configs are always in English.
- Change the skill workflow only in `.agents/skills/onionforge/SKILL.md` — the adapter in `.claude/skills/` must stay a thin pointer with no duplicated content.
- Files in `templates/` are copied verbatim into generated projects (with placeholder substitution) — keep them buildable and tool-agnostic.
