#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/../source/api"

: "${SONAR_HOST_URL:?Set SONAR_HOST_URL (e.g. http://localhost:9000)}"
: "${SONAR_TOKEN:?Set SONAR_TOKEN (generate one in SonarQube: My Account -> Security)}"

dotnet tool restore

dotnet sonarscanner begin \
  /k:"{{solution-name-lower}}-api" \
  /n:"{{SolutionName}} Api" \
  /d:sonar.host.url="$SONAR_HOST_URL" \
  /d:sonar.token="$SONAR_TOKEN" \
  /d:sonar.cs.opencover.reportsPaths="**/TestResults/**/coverage.opencover.xml"

dotnet build --no-incremental

dotnet test --no-build \
  --collect:"XPlat Code Coverage" \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

dotnet sonarscanner end /d:sonar.token="$SONAR_TOKEN"
