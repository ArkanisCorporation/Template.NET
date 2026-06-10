#!/usr/bin/env bash
set -eEuo pipefail

THIS_DIR="$(dirname "$(realpath "$0")")"

. "${THIS_DIR}/prepare-common.sh"

# Optional server container image creation.
# prepare_docker_image "Template" "${$(default_docker_image_bare)}"
