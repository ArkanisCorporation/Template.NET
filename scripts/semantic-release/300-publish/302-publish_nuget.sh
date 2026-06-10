#!/usr/bin/env bash
set -eEuo pipefail

THIS_DIR="$(dirname "$(realpath "$0")")"

. "${THIS_DIR}/publish-common.sh"

# NuGet package publish.
# publish_nuget_packages "$(nuget_artifact_dir "template")"
