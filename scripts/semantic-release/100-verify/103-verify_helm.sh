#!/usr/bin/env bash
set -eEuo pipefail

THIS_DIR="$(dirname "$(realpath "$0")")"

. "${THIS_DIR}/verify-common.sh"

# Optional Helm chart verification.
if is_enabled "${RELEASE_HELM_ENABLED:-false}"; then
    verify_helm_release_target
else
    skip_release_target "Helm release target; RELEASE_HELM_ENABLED is not true"
fi
