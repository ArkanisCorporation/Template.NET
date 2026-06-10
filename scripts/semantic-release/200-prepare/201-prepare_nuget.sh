#!/usr/bin/env bash
set -eEuo pipefail

THIS_DIR="$(dirname "$(realpath "$0")")"

. "${THIS_DIR}/prepare-common.sh"

# NuGet artifact creation.
# prepare_nuget_package "Template" "$(nuget_artifact_dir "template")"
