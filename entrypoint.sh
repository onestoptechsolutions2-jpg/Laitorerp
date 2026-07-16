#!/usr/bin/env sh
set -e

dotnet Leitor.Erp.dll --migrate-database

exec dotnet Leitor.Erp.dll
