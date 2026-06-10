#!/usr/bin/env bash
set -eEuo pipefail

THIS_DIR="$(dirname "$(realpath "$0")")"

. "${THIS_DIR}/publish-common.sh"

# Optional server container publish.
# publish_docker_image "Template" "${$(default_docker_image_bare)}"
