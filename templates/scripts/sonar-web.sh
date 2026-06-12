#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/../source/web"

: "${SONAR_HOST_URL:?Set SONAR_HOST_URL (e.g. http://localhost:9000)}"
: "${SONAR_TOKEN:?Set SONAR_TOKEN (generate one in SonarQube: My Account -> Security)}"

npm run coverage

npx --yes sonarqube-scanner \
  -Dsonar.host.url="$SONAR_HOST_URL" \
  -Dsonar.token="$SONAR_TOKEN"
