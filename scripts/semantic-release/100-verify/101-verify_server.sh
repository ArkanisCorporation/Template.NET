#!/usr/bin/env bash
set -eEuo pipefail

THIS_DIR="$(dirname "$(realpath "$0")")"

. "${THIS_DIR}/verify-common.sh"

# Optional server container verification.
if [[ -z "${RELEASE_SERVER_PROJECT:-}" ]]; then
    skip_release_target "server release target; RELEASE_SERVER_PROJECT is not set"
else
    verify_docker_project "${RELEASE_SERVER_PROJECT}"
fi
