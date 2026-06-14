#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/../source/api"

: "${SONAR_HOST_URL:?Set SONAR_HOST_URL (e.g. http://localhost:9000)}"
: "${SONAR_TOKEN:?Set SONAR_TOKEN (generate one in SonarQube: My Account -> Security)}"

dotnet tool restore

# Analysis settings live in source/api/SonarQube.Analysis.xml (the .NET scanner
# does not read sonar-project.properties); only key, name and secrets stay here.
dotnet sonarscanner begin \
  /k:"{{solution-name-lower}}-api" \
  /n:"{{SolutionName}} Api" \
  /s:"$(pwd)/SonarQube.Analysis.xml" \
  /d:sonar.host.url="$SONAR_HOST_URL" \
  /d:sonar.token="$SONAR_TOKEN"

dotnet build --no-incremental

# Coverage settings (OpenCover format, exclusions) live in coverlet.runsettings.
dotnet test --no-build --settings coverlet.runsettings

dotnet sonarscanner end /d:sonar.token="$SONAR_TOKEN"
