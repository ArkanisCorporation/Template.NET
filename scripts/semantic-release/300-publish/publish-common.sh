#!/usr/bin/env bash
set -eEuo pipefail

THIS_DIR="$(dirname "$(realpath "${BASH_SOURCE[0]}")")"

. "${THIS_DIR}/../common.sh"

# Publishes all NuGet packages from one source directory.
function publish_nuget_packages() {
    if [[ $# -ne 1 ]]; then
        fail "usage: publish_nuget_packages <source-dir>"
    fi

    local source_dir="$1"

    require_env "NUGET_PUBLISH_API_KEY"
    : "${NUGET_PUBLISH_SOURCE_URL:=https://api.nuget.org/v3/index.json}"

    shopt -s nullglob
    local packages=("${source_dir}"/*.nupkg)
    shopt -u nullglob

    if [[ "${#packages[@]}" -eq 0 ]]; then
        fail "No NuGet packages found in ${source_dir}"
    fi

    for package in "${packages[@]}"; do
        >&2 echo "Pushing ${package} to ${NUGET_PUBLISH_SOURCE_URL}..."
        dotnet nuget push "${package}" \
            --source "${NUGET_PUBLISH_SOURCE_URL}" \
            --api-key "${NUGET_PUBLISH_API_KEY}" \
            --skip-duplicate \
            1>&2 # logging output must not go to stdout
    done

    >&2 echo "Successfully published ${#packages[@]} NuGet package(s)"
}

# Publishes one Docker image from a project Dockerfile.
function publish_docker_image() {
    if [[ $# -ne 2 ]]; then
        fail "usage: publish_docker_image <project-name> <image-name-bare>"
    fi

    require_env "VERSION_TAG"
    require_env "VERSION_CHANNEL"

    local project_name="$1"
    local image_name_bare="$2"
    local dockerfile_path="./src/${project_name}/Dockerfile"
    local image_name
    image_name="$(docker_image_name "${image_name_bare}")"
    local image_name_version_tagged="${image_name}:${VERSION_TAG}"
    local image_name_channel_tagged="${image_name}:${VERSION_CHANNEL}-latest"
    local image_name_latest_tagged="${image_name}:latest"

    if [[ ! -f "${dockerfile_path}" ]]; then
        fail "Dockerfile ${dockerfile_path} does not exist"
    fi

    >&2 echo "Publishing ${image_name} from ${dockerfile_path}..."
    >&2 echo "  as ${image_name_version_tagged}"
    >&2 echo "  as ${image_name_channel_tagged}"
    docker buildx build \
        --push \
        --cache-to type=gha \
        --tag "${image_name_version_tagged}" \
        --tag "${image_name_channel_tagged}" \
        --file "${dockerfile_path}" \
        --build-arg "BUILD_CONFIGURATION=${CONFIGURATION}" \
        . \
        1>&2 # logging output must not go to stdout

    if [[ "${VERSION_CHANNEL}" == "stable" ]]; then
        >&2 echo "  as ${image_name_latest_tagged}"
        docker buildx build \
            --push \
            --tag "${image_name_latest_tagged}" \
            --file "${dockerfile_path}" \
            --build-arg "BUILD_CONFIGURATION=${CONFIGURATION}" \
            . \
            1>&2 # logging output must not go to stdout
    fi

    >&2 echo "Successfully published ${image_name}"
}

# Publishes all Helm chart packages from one chart directory.
function publish_helm_charts() {
    if [[ $# -ne 1 ]]; then
        fail "usage: publish_helm_charts <chart-dir>"
    fi

    local chart_dir="$1"

    require_env "GITHUB_OWNER"

    local github_owner_bare
    github_owner_bare="$(echo "${GITHUB_OWNER}" | tr '[:upper:]' '[:lower:]')"

    shopt -s nullglob
    local charts=("${chart_dir}"/*.tgz)
    shopt -u nullglob

    if [[ "${#charts[@]}" -eq 0 ]]; then
        fail "No Helm charts found in ${chart_dir}"
    fi

    for chart in "${charts[@]}"; do
        >&2 echo "Publishing ${chart} Helm chart..."
        helm push "${chart}" "oci://ghcr.io/${github_owner_bare}/charts" \
            1>&2 # logging output must not go to stdout
    done

    >&2 echo "Successfully published ${#charts[@]} Helm chart(s)"
}

function skip_release_target() {
    if [[ $# -ne 1 ]]; then
        fail "usage: skip_release_target <message>"
    fi

    >&2 echo "Skipping $1"
}
