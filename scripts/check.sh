#!/usr/bin/env bash
set -euo pipefail

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
backend_solution="$project_root/backend/Fast.Workshops.sln"
frontend_root="$project_root/frontend"

node --test "$project_root/scripts/run-project.test.mjs" "$project_root/scripts/seed-mysql.test.mjs"

dotnet format "$backend_solution" --verify-no-changes --no-restore
dotnet build "$backend_solution" --no-restore
dotnet test "$backend_solution" --no-restore --no-build
npm --prefix "$frontend_root" run format:check
npm --prefix "$frontend_root" run lint
npm --prefix "$frontend_root" run build
npm --prefix "$frontend_root" test -- --watch=false

echo "Todas as verificações passaram."
