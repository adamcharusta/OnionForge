# Frontend — generation conventions (React + Vite)

Scaffold: `npm create vite@latest web -- --template react-ts`, then add the configuration below. The frontend always lives in `source/web/` (the fixed project layout is described in [backend.md](backend.md) and [../templates/MANIFEST.md](../templates/MANIFEST.md)).

## Fixed elements (always, without asking)

- **TypeScript** (strict),
- **Vitest** + Testing Library (`@testing-library/react`, `@testing-library/jest-dom`, `jsdom`),
- **TanStack Query** (`@tanstack/react-query`) for API communication,
- **Orval** — the typed API client and TanStack Query hooks are **generated**, not hand-written (see below),
- **ESLint (flat config) + Prettier** — full set below.

User's choice: **Tailwind CSS** vs **MUI** (fixed question) + dynamic libraries from the consultation (forms, routing, tables etc.).

## Invariant files — copy from templates

`eslint.config.js`, `.prettierrc`, `vite.config.ts` (placeholder `{{ApiPort}}`; includes Vitest + coverage config), `orval.config.ts`, `src/test/setup.ts`, `src/api/client.ts`, `.gitignore`, `.dockerignore`, `Dockerfile`, `nginx.conf` and `sonar-project.properties` are ready in [../templates/web/](../templates/web/) — copy them according to [../templates/MANIFEST.md](../templates/MANIFEST.md), do not write them from scratch. The target `package.json` script set is documented there as well.

DevDependencies required by these configs (exactly this set):

```
@eslint/js, typescript-eslint, eslint, eslint-plugin-react, eslint-plugin-react-hooks,
eslint-plugin-react-refresh, eslint-plugin-jsx-a11y, eslint-plugin-simple-import-sort,
eslint-config-prettier, prettier, globals, orval,
vitest, @vitest/coverage-v8, jsdom, @testing-library/react, @testing-library/jest-dom
```

## API client — generated with Orval

The backend build emits an OpenAPI document to `source/api/openapi/` (see [backend.md](backend.md)). `orval.config.ts` (template) turns it into a typed client + TanStack Query hooks:

- `npm run api:generate` (also hooked as `predev`) writes `src/api/generated/` — endpoints with `useQuery`/`useMutation` hooks and model types. Regenerate after any backend contract change.
- The generated output is **committed** — `npm run build` and the Docker image build never need the backend spec.
- All generated calls go through `src/api/client.ts` (the Orval mutator) — one place for ProblemDetails handling and `ApiError`.
- Never edit files under `src/api/generated/` by hand; exclude the directory from ESLint and Sonar analysis (`sonar.exclusions`).

## Docker and SonarQube

- `Dockerfile` is a multi-stage build: `node:22-alpine` builds the app, `nginx:alpine` serves `dist/`; `nginx.conf` proxies `/api/` to the `api` compose service and provides the SPA fallback.
- Sonar uses `sonar-project.properties` (project key `{{solution-name-lower}}-web`) with lcov coverage from `npm run coverage`; `scripts/sonar-web.sh` runs the whole cycle (requires `SONAR_HOST_URL` and `SONAR_TOKEN`).

## Directory structure (feature-based)

```
src/
├── api/
│   ├── client.ts        # ready in templates: fetch wrapper (ProblemDetails + ApiError), used as the Orval mutator
│   └── generated/       # Orval output (endpoints + TanStack Query hooks + model types) — committed, never hand-edited
├── features/
│   └── {feature}/       # per business feature
│       ├── hooks.ts     # thin wrappers/re-exports over the generated hooks (feature-level options, invalidations)
│       ├── components/
│       └── {Feature}Page.tsx
├── components/          # shared UI components
├── lib/                 # utils
├── test/setup.ts
├── App.tsx              # QueryClientProvider (+ router, if chosen)
└── main.tsx
```

## TanStack Query — pattern

Orval generates the `useQuery`/`useMutation` hooks and their query keys. Feature code wraps them only where it adds behavior (e.g. invalidation after a mutation):

```ts
import { getTodosQueryKey, useCreateTodo as useCreateTodoGenerated, useGetTodos } from '@/api/generated/endpoints';

export const useTodos = useGetTodos;

export function useCreateTodo() {
  const queryClient = useQueryClient();
  return useCreateTodoGenerated({
    mutation: {
      onSuccess: () => queryClient.invalidateQueries({ queryKey: getTodosQueryKey() }),
    },
  });
}
```

Environment configuration: `VITE_API_URL` in `.env.development` (+ `.env.example`); in dev, the Vite proxy handles `/api`.

## Example feature

For the entity chosen during the consultation, generate: a list page (useQuery) + a create form (useMutation, validation) + at least one Vitest component test (render with `QueryClientProvider`, the `src/api/client.ts` module mocked so the generated hooks run against mocked responses).
