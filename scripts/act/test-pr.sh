#!/usr/bin/env bash
set -euo pipefail

# Bash expansion note: ${ACT_BIN:-} safely expands to empty when ACT_BIN is unset under set -u.
# Related pattern forms such as ${value%%/*} remove the longest suffix matching /*.
if [[ -n "${ACT_BIN:-}" ]]; then
    act_bin="${ACT_BIN}"
elif command -v act >/dev/null 2>&1; then
    act_bin="act"
elif act_exe="$(command -v act.exe 2>/dev/null)" && "${act_exe}" --version >/dev/null 2>&1; then
    act_bin="${act_exe}"
else
    echo "act executable not found. Install act or set ACT_BIN to its executable path." >&2
    exit 127
fi

"${act_bin}" pull_request \
    -W .github/workflows/main.yaml \
    -j test \
    -e .act/events/pull_request.json
