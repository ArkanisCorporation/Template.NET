#!/usr/bin/env bash
set -eEuo pipefail

THIS_DIR="$(dirname "$(realpath "$0")")"

. "${THIS_DIR}/prepare-common.sh"

# Optional Helm chart artifact creation.
if is_enabled "${RELEASE_HELM_ENABLED:-false}"; then
    prepare_helm_chart "${RELEASE_HELM_MANIFEST_DIR}" "${RELEASE_HELM_CHART_DIR}"
else
    skip_release_target "Helm chart build; RELEASE_HELM_ENABLED is not true"
fi
