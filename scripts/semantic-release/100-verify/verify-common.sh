#!/usr/bin/env bash
set -eEuo pipefail

THIS_DIR="$(dirname "$(realpath "${BASH_SOURCE[0]}")")"

. "${THIS_DIR}/../common.sh"

# Verifies a NuGet package project exists before prepare builds it.
function verify_nuget_project() {
    if [[ $# -ne 1 ]]; then
        fail "usage: verify_nuget_project <project-name>"
    fi

    local project_name="$1"
    local project_file="./src/${project_name}/${project_name}.csproj"

    require_command "dotnet"

    if [[ ! -f "${project_file}" ]]; then
        fail "NuGet project file ${project_file} does not exist"
    fi

    >&2 echo "Verified NuGet release target ${project_name}"
}

# Verifies a Docker release project exists before prepare builds its image.
function verify_docker_project() {
    if [[ $# -ne 1 ]]; then
        fail "usage: verify_docker_project <project-name>"
    fi

    local project_name="$1"
    local dockerfile_path="./src/${project_name}/Dockerfile"

    require_command "docker"

    if [[ ! -f "${dockerfile_path}" ]]; then
        fail "Dockerfile ${dockerfile_path} does not exist"
    fi

    >&2 echo "Verified Docker release target ${project_name}"
}

# Verifies optional Helm release tooling before prepare builds chart artifacts.
function verify_helm_release_target() {
    require_command "dotnet"
    require_command "helm"
    require_env "GITHUB_OWNER"

    >&2 echo "Verified Helm release target"
}

# Keeps optional targets easy to skip from leaf scripts.
function skip_release_target() {
    if [[ $# -ne 1 ]]; then
        fail "usage: skip_release_target <message>"
    fi

    >&2 echo "Skipping $1"
}
