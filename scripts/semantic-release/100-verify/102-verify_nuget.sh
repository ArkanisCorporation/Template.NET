#!/usr/bin/env bash
set -eEuo pipefail

THIS_DIR="$(dirname "$(realpath "$0")")"

. "${THIS_DIR}/verify-common.sh"

# NuGet project verification.
# verify_nuget_project "Template"
