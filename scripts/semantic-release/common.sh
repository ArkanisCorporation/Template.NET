#!/usr/bin/env bash
set -eEuo pipefail

# Shared defaults and helpers for all semantic-release shell phases.
function configure_release_defaults() {
    : "${CONFIGURATION:=Release}"
    : "${REGISTRY:=ghcr.io}"

    : "${RELEASE_ARTIFACTS_DIR:=artifacts}"
    : "${RELEASE_NUGET_ARTIFACTS_DIR:=${RELEASE_ARTIFACTS_DIR}/nuget}"
    : "${RELEASE_HELM_MANIFEST_DIR:=${RELEASE_ARTIFACTS_DIR}/helm/manifest}"
    : "${RELEASE_HELM_CHART_DIR:=${RELEASE_ARTIFACTS_DIR}/helm/chart}"

    if [[ -z "${GITHUB_OWNER+x}" && -n "${GITHUB_REPOSITORY+x}" ]]; then
        GITHUB_OWNER="${GITHUB_REPOSITORY%%/*}"
    fi
}

function fail() {
    >&2 echo "$1"
    exit 2
}

function require_env() {
    local name="$1"

    if [[ -z "${!name:-}" ]]; then
        fail "${name} is not set"
    fi
}

function require_command() {
    local name="$1"

    if ! command -v "${name}" >/dev/null 2>&1; then
        fail "${name} is not available"
    fi
}

function is_enabled() {
    [[ "$1" == "true" || "$1" == "1" || "$1" == "yes" ]]
}

function lower() {
    echo "$1" | tr '[:upper:]' '[:lower:]'
}

function nuget_artifact_dir() {
    if [[ $# -ne 1 ]]; then
        fail "usage: nuget_artifact_dir <package-name>"
    fi

    echo "${RELEASE_NUGET_ARTIFACTS_DIR}/$1"
}

function docker_image_name() {
    if [[ $# -ne 1 ]]; then
        fail "usage: docker_image_name <image-name-bare>"
    fi

    echo "${REGISTRY}/$(lower "$1")"
}

function default_docker_image_bare() {
    require_env "GITHUB_REPOSITORY"
    lower "${GITHUB_REPOSITORY}"
}

function run_sub() {
    if [[ $# -ne 1 ]]; then
        fail "usage: run_sub <script>"
    fi

    local script="$1"
    local status=0

    >&2 echo "running ${script}"

    "${script}" || status=$?

    if [[ "${status}" -ne 0 ]]; then
        >&2 echo "failed running sub-script ${script}, exited with ${status}"
        exit 2
    fi
}

function run_subs_with_prefix() {
    if [[ $# -ne 2 ]]; then
        >&2 echo "usage: run_subs_with_prefix <directory> <prefix>"
        exit 2
    fi

    local directory="$1"
    local prefix="$2"

    if [[ ! -d "${directory}" ]]; then
        >&2 echo "directory ${directory} does not exist"
        exit 2
    fi

    local scripts=()
    while IFS= read -r -d "" script; do
        scripts+=("${script}")
    done < <(find "${directory}" -maxdepth 1 -type f -name "${prefix}*.sh" -print0 | sort -z)

    if [[ "${#scripts[@]}" -eq 0 ]]; then
        >&2 echo "no sub-scripts found in ${directory} with prefix ${prefix}"
        return 0
    fi

    for script in "${scripts[@]}"; do
        run_sub "${script}"
    done
}

configure_release_defaults
