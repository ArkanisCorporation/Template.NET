#!/usr/bin/env bash
set -eEuo pipefail

THIS_DIR="$(dirname "$(realpath "${BASH_SOURCE[0]}")")"

. "${THIS_DIR}/../common.sh"

# Builds one NuGet package into the provided output directory.
function prepare_nuget_package() {
    if [[ $# -ne 2 ]]; then
        fail "usage: prepare_nuget_package <project-name> <output-dir>"
    fi

    local project_name="$1"
    local output_dir="$2"
    local project_file="./src/${project_name}/${project_name}.csproj"

    if [[ ! -f "${project_file}" ]]; then
        fail "NuGet project file ${project_file} does not exist"
    fi

    >&2 echo "Building ${project_name} NuGet package..."
    dotnet pack "${project_file}" \
        --configuration "${CONFIGURATION}" \
        --output "${output_dir}" \
        --include-symbols \
        --include-source \
        --no-restore \
        1>&2 # logging output must not go to stdout

    >&2 echo "Successfully built the ${project_name} NuGet package"
}

# Builds one Docker image from a project Dockerfile.
function prepare_docker_image() {
    if [[ $# -ne 2 ]]; then
        fail "usage: prepare_docker_image <project-name> <image-name-bare>"
    fi

    local project_name="$1"
    local image_name_bare="$2"
    local dockerfile_path="./src/${project_name}/Dockerfile"
    local image_name
    image_name="$(docker_image_name "${image_name_bare}")"

    if [[ ! -f "${dockerfile_path}" ]]; then
        fail "Dockerfile ${dockerfile_path} does not exist"
    fi

    >&2 echo "Building ${image_name} from ${dockerfile_path}..."
    docker buildx build \
        --load \
        --cache-from type=gha \
        --cache-from "${image_name}" \
        --cache-to type=inline,mode=max \
        --tag "${image_name}" \
        --file "${dockerfile_path}" \
        --build-arg "BUILD_CONFIGURATION=${CONFIGURATION}" \
        . \
        1>&2 # logging output must not go to stdout

    >&2 echo "Successfully built ${image_name}"
}

# Builds, lints, and packages one Helm chart.
function prepare_helm_chart() {
    if [[ $# -ne 2 ]]; then
        fail "usage: prepare_helm_chart <manifest-dir> <chart-dir>"
    fi

    local manifest_dir="$1"
    local chart_dir="$2"

    >&2 echo "Building the Helm chart manifest..."
    dotnet aspire publish --output-path "${manifest_dir}" --environment Kubernetes \
        1>&2 # logging output must not go to stdout

    >&2 echo "Verifying the Helm chart manifest..."
    helm lint "${manifest_dir}" \
        1>&2 # logging output must not go to stdout

    >&2 echo "Packaging the Helm chart..."
    helm package "${manifest_dir}" --destination "${chart_dir}" \
        1>&2 # logging output must not go to stdout

    >&2 echo "Successfully prepared and verified the Helm chart"
}

function skip_release_target() {
    if [[ $# -ne 1 ]]; then
        fail "usage: skip_release_target <message>"
    fi

    >&2 echo "Skipping $1"
}
