#!/usr/bin/env bash
set -eEuo pipefail

THIS_DIR="$(dirname "$(realpath "$0")")"

. "${THIS_DIR}/publish-common.sh"

# Optional Helm chart publish.
if is_enabled "${RELEASE_HELM_ENABLED:-false}"; then
    publish_helm_charts "${RELEASE_HELM_CHART_DIR}"
else
    skip_release_target "Helm chart publish; RELEASE_HELM_ENABLED is not true"
fi
