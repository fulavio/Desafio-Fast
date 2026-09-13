#!/usr/bin/env bash
set -euo pipefail

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
backend_solution="$project_root/backend/Fast.Workshops.sln"
frontend_package="$project_root/frontend/package.json"

command -v dotnet >/dev/null || { echo "Erro: dotnet não encontrado no PATH." >&2; exit 1; }
command -v npm >/dev/null || { echo "Erro: npm não encontrado no PATH." >&2; exit 1; }
[[ -f "$backend_solution" ]] || { echo "Erro: solução não encontrada em $backend_solution." >&2; exit 1; }
[[ -f "$frontend_package" ]] || { echo "Erro: frontend não encontrado em $frontend_package." >&2; exit 1; }

dotnet restore "$backend_solution"
npm --prefix "$project_root/frontend" ci

echo "Setup concluído. Execute ./scripts/check.sh."
