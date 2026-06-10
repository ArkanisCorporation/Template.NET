#!/usr/bin/env bash
set -eEuo pipefail

THIS_DIR="$(dirname "$(realpath "$0")")"

. "${THIS_DIR}/prepare-common.sh"

# Optional server container image creation.
if [[ -z "${RELEASE_SERVER_PROJECT:-}" ]]; then
    skip_release_target "server image build; RELEASE_SERVER_PROJECT is not set"
else
    prepare_docker_image "${RELEASE_SERVER_PROJECT}" "${RELEASE_DOCKER_IMAGE_BARE:-$(default_docker_image_bare)}"
fi
