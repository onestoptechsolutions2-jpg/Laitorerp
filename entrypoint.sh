#!/usr/bin/env sh
set -e

# openiddict.pfx is never committed to git (this repo is public) - it's supplied as a
# base64-encoded secret env var and materialized here at container startup instead.
# Stripping whitespace and ignoring garbage characters guards against line-wrapping or
# stray whitespace introduced when pasting this long value into a UI textarea.
if [ -n "$OPENIDDICT_CERT_BASE64" ]; then
    printf '%s' "$OPENIDDICT_CERT_BASE64" | tr -d '[:space:]' | base64 -d -i > /app/openiddict.pfx
    echo "openiddict.pfx written: $(wc -c < /app/openiddict.pfx) bytes (expected 2563)"
fi

dotnet Leitor.Erp.dll --migrate-database

exec dotnet Leitor.Erp.dll
