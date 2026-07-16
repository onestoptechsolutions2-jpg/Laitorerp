#!/usr/bin/env sh
set -e

# openiddict.pfx is never committed to git (this repo is public) - it's supplied as a
# base64-encoded secret env var and materialized here at container startup instead.
if [ -n "$OPENIDDICT_CERT_BASE64" ]; then
    echo "$OPENIDDICT_CERT_BASE64" | base64 -d > /app/openiddict.pfx
fi

dotnet Leitor.Erp.dll --migrate-database

exec dotnet Leitor.Erp.dll
