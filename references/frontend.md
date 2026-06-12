# Frontend — generation conventions (React + Vite)

Scaffold: `npm create vite@latest web -- --template react-ts`, then add the configuration below. The frontend always lives in `source/web/` (the fixed project layout is described in [backend.md](backend.md) and [../templates/MANIFEST.md](../templates/MANIFEST.md)).

## Fixed elements (always, without asking)

- **TypeScript** (strict),
- **Vitest** + Testing Library (`@testing-library/react`, `@testing-library/jest-dom`, `jsdom`),
- **TanStack Query** (`@tanstack/react-query`) for API communication,
- **ESLint (flat config) + Prettier** — full set below.

User's choice: **Tailwind CSS** vs **MUI** (fixed question) + dynamic libraries from the consultation (forms, routing, tables etc.).

## Invariant files — copy from templates

`eslint.config.js`, `.prettierrc`, `vite.config.ts` (placeholder `{{ApiPort}}`; includes Vitest + coverage config), `src/test/setup.ts`, `src/api/client.ts`, `.gitignore`, `.dockerignore`, `Dockerfile`, `nginx.conf` and `sonar-project.properties` are ready in [../templates/web/](../templates/web/) — copy them according to [../templates/MANIFEST.md](../templates/MANIFEST.md), do not write them from scratch. The target `package.json` script set is documented there as well.

DevDependencies required by these configs (exactly this set):

```
@eslint/js, typescript-eslint, eslint, eslint-plugin-react, eslint-plugin-react-hooks,
eslint-plugin-react-refresh, eslint-plugin-jsx-a11y, eslint-plugin-simple-import-sort,
eslint-config-prettier, prettier, globals,
vitest, @vitest/coverage-v8, jsdom, @testing-library/react, @testing-library/jest-dom
```

## Docker and SonarQube

- `Dockerfile` is a multi-stage build: `node:22-alpine` builds the app, `nginx:alpine` serves `dist/`; `nginx.conf` proxies `/api/` to the `api` compose service and provides the SPA fallback.
- Sonar uses `sonar-project.properties` (project key `{{solution-name-lower}}-web`) with lcov coverage from `npm run coverage`; `scripts/sonar-web.sh` runs the whole cycle (requires `SONAR_HOST_URL` and `SONAR_TOKEN`).

## Directory structure (feature-based)

```
src/
├── api/                 # HTTP client + response types
│   ├── client.ts        # ready in templates: fetch wrapper with ProblemDetails handling and an ApiError class
│   └── types.ts
├── features/
│   └── {feature}/       # per business feature
│       ├── api.ts       # functions calling the endpoints
│       ├── hooks.ts     # useQuery/useMutation (query keys in constants)
│       ├── components/
│       └── {Feature}Page.tsx
├── components/          # shared UI components
├── lib/                 # utils
├── test/setup.ts
├── App.tsx              # QueryClientProvider (+ router, if chosen)
└── main.tsx
```

## TanStack Query — pattern

```ts
export const todoKeys = {
  all: ['todos'] as const,
  detail: (id: string) => ['todos', id] as const,
};

export function useTodos() {
  return useQuery({ queryKey: todoKeys.all, queryFn: fetchTodos });
}

export function useCreateTodo() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createTodo,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: todoKeys.all }),
  });
}
```

Environment configuration: `VITE_API_URL` in `.env.development` (+ `.env.example`); in dev, the Vite proxy handles `/api`.

## Example feature

For the entity chosen during the consultation, generate: a list page (useQuery) + a create form (useMutation, validation) + at least one Vitest component test (render with `QueryClientProvider`, the `api.ts` module mocked).
