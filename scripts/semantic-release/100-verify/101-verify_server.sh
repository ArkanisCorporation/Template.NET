#!/usr/bin/env bash
set -eEuo pipefail

THIS_DIR="$(dirname "$(realpath "$0")")"

. "${THIS_DIR}/verify-common.sh"

# Optional server container verification.
# verify_docker_project "Template"
