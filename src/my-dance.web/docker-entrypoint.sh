#!/bin/sh
# Generates the runtime config the SPA loads at startup, from environment
# variables, so the same image can be deployed to any environment.
# Runs via the nginx image's /docker-entrypoint.d hook before nginx starts.
set -e

API_BASE_URL="${API_BASE_URL:-${REACT_APP_API_BASE_URL:-https://localhost:7068}}"
AUTH_URL="${AUTH_URL:-${REACT_APP_AUTH_URL:-https://localhost:7259}}"
REDIRECT_URI="${REDIRECT_URI:-${REACT_APP_REDIRECT_URI:-}}"

cat > /usr/share/nginx/html/config.json <<EOF
{
  "apiBaseUrl": "${API_BASE_URL}",
  "authUrl": "${AUTH_URL}",
  "redirectUri": "${REDIRECT_URI}"
}
EOF

echo "Wrote /usr/share/nginx/html/config.json (apiBaseUrl=${API_BASE_URL}, authUrl=${AUTH_URL})"
