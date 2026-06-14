# Commit & versioning workflow

How the agent sets up commit conventions and versioning in a generated project, based on the
consultation answer (Step 3, fixed decision "Commit & versioning workflow"). The tiers are
cumulative: tier 2 includes tier 1, tier 3 includes tier 1 and 2. Default is **tier 1**.

Everything here is free / permissive-licensed: Conventional Commits (spec), Husky.Net (MIT),
MinVer (MIT), Release Please (Apache-2.0).

| Tier | Adds | Couples to |
|---|---|---|
| 0 — None | plain git (still `.gitattributes` + `first commit`) | — |
| 1 — Convention + hooks (default) | Conventional Commits + Husky.Net (`commit-msg` lint, `pre-commit` format) | nothing (local, cross-platform) |
| 2 — + Versioning from tags | MinVer for .NET, web app version from the same git tag | nothing |
| 3 — + CI automation | Release Please (auto bump + CHANGELOG + tag + release) | GitHub Actions |

The chosen tier and its rationale are recorded in `docs/adr/0001-technology-stack.md`.

---

## Tier 1 — Conventional Commits + Husky.Net

Git hooks live at the repository root and must cover both subprojects, so Husky.Net (a .NET
tool — the SDK is always present) is installed at the **project root**, not under `source/api`.

Pick a **hook runner** (consultation sub-question). Both enforce the same Conventional Commits rules
via `commit-msg`, and format staged `.cs` via `pre-commit`. Default **Husky.Net**.

| | Husky.Net (default) | Classic Husky (Node) |
|---|---|---|
| Tooling | `dotnet` tool, bundled `.csx` linter | npm: husky + commitlint + lint-staged |
| Root footprint | `.config/dotnet-tools.json` (already common) | **adds a root `package.json` + `node_modules`** |
| Why | SDK always present; nothing extra at root | familiar npm/commitlint ecosystem; `lint-staged` also formats staged web files |

#### 1a — Husky.Net (default)

Files (copy from `templates/git/husky/`):

| Template | Target |
|---|---|
| `templates/git/husky/task-runner.json` | `.husky/task-runner.json` |
| `templates/git/husky/commit-lint.csx` | `.husky/csx/commit-lint.csx` |
| `templates/git/husky/commit-msg` | `.husky/commit-msg` |
| `templates/git/husky/pre-commit` | `.husky/pre-commit` |

`task-runner.json` has the `{{SolutionName}}` placeholder (the `dotnet format` target points at
`source/api/{{SolutionName}}.slnx`). The hook scripts and the `.csx` are copied verbatim.

Setup (project root, Step 7 — after `git init`):

```bash
dotnet new tool-manifest          # creates ./.config/dotnet-tools.json (skip if it exists)
dotnet tool install husky         # pins the current Husky.Net version into the manifest
dotnet husky install              # generates .husky/_/husky.sh and sets core.hooksPath=.husky
```

`dotnet husky install` regenerates the internal `.husky/_/` folder; the four files above are the
project-specific config layered on top of it. Run the install **before** copying `task-runner.json`
and the hooks, then overwrite the defaults Husky created.

#### 1b — Classic Husky (Node)

Git hooks live at the repo root, but Node otherwise lives only in `source/web`, so this variant adds
a **tooling-only root `package.json`** (not an application package).

Files (copy from `templates/git/husky-node/`):

| Template | Target |
|---|---|
| `templates/git/husky-node/package.json` | `package.json` (root; placeholder `{{solution-name-lower}}`) |
| `templates/git/husky-node/commitlint.config.js` | `commitlint.config.js` |
| `templates/git/husky-node/lintstagedrc.mjs` | `.lintstagedrc.mjs` (placeholder `{{SolutionName}}`) |
| `templates/git/husky-node/commit-msg` | `.husky/commit-msg` |
| `templates/git/husky-node/pre-commit` | `.husky/pre-commit` |

`lint-staged` runs `dotnet format` on staged `.cs` and `prettier` on staged web files (via
`npm --prefix source/web exec`, so prettier is not duplicated at the root).

Setup (project root, Step 7 — after `git init`):

```bash
npm install                       # installs husky/commitlint/lint-staged; its prepare runs husky
```

`npm install` runs the `prepare` script (`husky`), which creates `.husky/_/` and sets
`core.hooksPath`. Copy the two hook files afterwards. **Add `/node_modules` to the root `.gitignore`**
(the base root `.gitignore` does not ignore a root `node_modules` — only the subprojects do).

### The scaffold commit

The `commit-msg` hook enforces Conventional Commits, but the required first commit message is the
literal `first commit`, which is **not** conventional. Make the scaffold commit bypass the hook so
the message stays as requested and the husky files are included in it:

```bash
git add -A
git commit --no-verify -m "first commit"
```

From the second commit on, the hook applies and developers must use Conventional Commits.

### What the hooks do

- `commit-msg` → runs `.husky/csx/commit-lint.csx` (regex over the Conventional Commits types:
  `build, chore, ci, docs, feat, fix, perf, refactor, revert, style, test`). Rejects anything else.
- `pre-commit` → `dotnet format` on staged `.cs` files only (fast, no Node dependency). Web linting
  stays in `npm run lint`; a developer can add a staged-web task to `task-runner.json` if wanted.

---

## Tier 2 — Versioning from git tags (MinVer)

One tag (`v1.2.3`) at the repo root = the version of the whole product. MinVer reads it for .NET
automatically; the web app reads the same tag at build time.

### Backend — MinVer

Central package management is on (`ManagePackageVersionsCentrally`), so the version goes in
`Directory.Packages.props` and the reference (without a version) in `Directory.Build.props`.

In the generated `source/api/Directory.Packages.props`, add:

```xml
<PackageVersion Include="MinVer" Version="6.0.0" />
```

In `templates/api/Directory.Build.props` (the copied template), add an `ItemGroup` + the tag prefix
to the `PropertyGroup`:

```xml
<PropertyGroup>
  <!-- ...existing properties... -->
  <MinVerTagPrefix>v</MinVerTagPrefix>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="MinVer" PrivateAssets="all" />
</ItemGroup>
```

With no tags yet, MinVer produces `0.0.0-alpha.0.N`. After `git tag v0.1.0`, the build stamps
`0.1.0`. MinVer is branch-agnostic; it only reads tags.

### Frontend — same tag

Expose the version through Vite's `define` so the web app can render it. In `vite.config.ts`:

```ts
import { execSync } from 'node:child_process';

const appVersion = (() => {
  try {
    return execSync('git describe --tags --always --dirty').toString().trim();
  } catch {
    return '0.0.0-dev';
  }
})();

export default defineConfig({
  // ...
  define: { __APP_VERSION__: JSON.stringify(appVersion) },
});
```

Declare `declare const __APP_VERSION__: string;` in `src/vite-env.d.ts`.

### CI note

Any pipeline that builds the project must check out the **full** git history
(`fetch-depth: 0` in GitHub Actions / `fetchDepth: 0` in Azure DevOps), or MinVer falls back to the
default version because it cannot see the tags.

---

## Tier 3 — Release automation (Release Please)

Adds a GitHub Actions workflow that, from Conventional Commits on the default branch, keeps a
"release PR" open with the next version bump and the generated `CHANGELOG.md`. Merging that PR
creates the git tag and GitHub Release — and the tag is exactly what MinVer (tier 2) consumes.

GitHub-specific. For GitLab/Bitbucket, document `semantic-release` or `git-cliff` instead; the
template ships the Release Please variant only.

### Files (copy from `templates/git/release-please/`)

| Template | Target |
|---|---|
| `templates/git/release-please/workflow.yml` | `.github/workflows/release-please.yml` |
| `templates/git/release-please/release-please-config.json` | `release-please-config.json` |
| `templates/git/release-please/release-please-manifest.json` | `.release-please-manifest.json` |

Uses the `simple` release type (language-agnostic): Release Please maintains a `version.txt` +
`CHANGELOG.md` and tags `v<version>`. The single tag drives both the .NET (MinVer) and web versions,
so there is no per-subproject version drift.

The workflow needs `contents: write` and `pull-requests: write` permissions (set in the file). No
secrets beyond the default `GITHUB_TOKEN`.
