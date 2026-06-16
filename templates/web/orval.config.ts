import { defineConfig } from 'orval';

// Generates the typed API client + TanStack Query hooks from the OpenAPI document
// emitted by the backend build (source/api/openapi/). Run with `npm run api:generate`
// (also runs automatically before `npm run dev`). The generated output is committed,
// so CI and the Docker build do not need the backend spec.
export default defineConfig({
  api: {
    input: '../api/openapi/{{SolutionName}}.Api.json',
    output: {
      target: 'src/api/generated/endpoints.ts',
      schemas: 'src/api/generated/model',
      client: 'react-query',
      httpClient: 'fetch',
      clean: true,
      prettier: true,
      override: {
        // The mutator returns the parsed response body, so generated types must not
        // wrap it in { data, status, headers }.
        fetch: {
          includeHttpResponseReturnType: false,
        },
        mutator: {
          path: 'src/api/client.ts',
          name: 'api',
        },
      },
    },
  },
});
