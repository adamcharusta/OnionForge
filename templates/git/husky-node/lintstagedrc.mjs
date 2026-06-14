// Staged-file formatting. Backend uses dotnet format; frontend runs prettier from
// source/web's own install (no prettier dependency is duplicated at the repo root).
export default {
  'source/api/**/*.cs': (files) =>
    `dotnet format source/api/{{SolutionName}}.slnx --include ${files.join(' ')}`,
  'source/web/**/*.{ts,tsx,js,jsx,css,json}': (files) =>
    `npm --prefix source/web exec -- prettier --write ${files.join(' ')}`,
};
