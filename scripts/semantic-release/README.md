# Semantic Release Scripts

This directory contains the shell scripts used by [`@semantic-release/exec`](https://github.com/semantic-release/exec).
The scripts are split by semantic-release lifecycle phase.

## Entry Points

[`release.config.mjs`](../../release.config.mjs) calls these entry points.

- [`100-verify/verify.sh`](100-verify/verify.sh) runs `verifyReleaseCmd`.
- [`200-prepare/prepare.sh`](200-prepare/prepare.sh) runs `prepareCmd`.
- [`300-publish/publish.sh`](300-publish/publish.sh) runs `publishCmd`.

Each entry point loads shared helpers, validates the semantic-release environment, and then runs phase sub-scripts by prefix.

## Script Ordering

Sub-scripts are selected by filename prefix.
The common helper `run_subs_with_prefix <directory> <prefix>` finds matching `*.sh` files directly in the provided directory, sorts them by filename, and runs them in order.

Current prefixes are:

- `1` for verify sub-scripts.
- `2` for prepare sub-scripts.
- `3` for publish sub-scripts.

For example, `300-publish/publish.sh` runs `301-publish_server.sh`, `302-publish_nuget.sh`, and `303-publish_helm.sh` in that order.

## Phase Responsibilities

The verify phase checks that configured targets and required tools are available.
It should not create release artifacts.

The prepare phase applies the release version and creates artifacts.
It runs `dotnet setversion --recursive "$VERSION"` before running prepare sub-scripts.

The publish phase publishes artifacts created during prepare.
It should consume existing artifacts rather than creating new ones.

## Defaults

Shared defaults are defined in [`common.sh`](common.sh).
These values describe common output roots and tool settings.
They are not intended to limit the release to one project, one package, or one image.

| Variable | Default | Purpose |
| --- | --- | --- |
| `CONFIGURATION` | `Release` | .NET build configuration. |
| `REGISTRY` | `ghcr.io` | Container registry used by Docker publish scripts. |
| `RELEASE_ARTIFACTS_DIR` | `artifacts` | Root directory for release artifacts. |
| `RELEASE_NUGET_ARTIFACTS_DIR` | `artifacts/nuget` | Root directory for NuGet package outputs. |
| `RELEASE_HELM_MANIFEST_DIR` | `artifacts/helm/manifest` | Helm manifest directory created by Aspire publish. |
| `RELEASE_HELM_CHART_DIR` | `artifacts/helm/chart` | Helm package directory used by prepare and publish. |

Downstream repositories should override these values in CI when the template defaults do not match the project.

## Required Environment

semantic-release passes these values into all configured exec commands.

- `VERSION` is the next semantic version.
- `VERSION_TAG` is the next Git tag.
- `VERSION_CHANNEL` is the semantic-release channel.

Publishing NuGet packages requires `NUGET_PUBLISH_API_KEY`.
`NUGET_PUBLISH_SOURCE_URL` defaults to `https://api.nuget.org/v3/index.json`.

Publishing Helm charts requires `GITHUB_OWNER`.
When `GITHUB_REPOSITORY` is available, `common.sh` derives `GITHUB_OWNER` from it.

Docker image helpers accept an image name argument.
When a leaf script wants to default to the current GitHub repository name, it can call `default_docker_image_bare`.

The template leaf scripts use `RELEASE_SERVER_PROJECT` as an optional switch for Docker image targets.
When it is empty, the default server verify, prepare, and publish scripts skip Docker work.
Set `RELEASE_DOCKER_IMAGE_BARE` to override the default image name used by those leaf scripts.

The template leaf scripts use `RELEASE_HELM_ENABLED=true` as an optional switch for Helm targets.
When it is not enabled, the default Helm verify, prepare, and publish scripts skip Helm work.

## Manual Runs

Restore local tools before running the release scripts manually.

```bash
dotnet tool restore
```

Run the verify phase with sample semantic-release values.

```bash
VERSION=1.2.3 \
VERSION_TAG=v1.2.3 \
VERSION_CHANNEL=ci \
./scripts/semantic-release/100-verify/verify.sh
```

Run the prepare phase with sample semantic-release values.

```bash
VERSION=1.2.3 \
VERSION_TAG=v1.2.3 \
VERSION_CHANNEL=ci \
./scripts/semantic-release/200-prepare/prepare.sh
```

Run the publish phase only after prepare has created artifacts.

```bash
VERSION=1.2.3 \
VERSION_TAG=v1.2.3 \
VERSION_CHANNEL=ci \
NUGET_PUBLISH_API_KEY=example \
./scripts/semantic-release/300-publish/publish.sh
```

Use a non-production NuGet source when testing publish locally.

```bash
NUGET_PUBLISH_SOURCE_URL=https://apiint.nugettest.org/v3/index.json
```

## Adding Release Targets

Add a verify script when a target has prerequisites that can fail early.
Name it with a `1` prefix.

Add a prepare script when a target creates artifacts.
Name it with a `2` prefix.

Add a publish script when a target uploads artifacts.
Name it with a `3` prefix.

The shared helper functions are reusable and accept the target as an argument.
This keeps each leaf script free to handle one or more projects explicitly.

To verify more NuGet projects, add calls like this to [`100-verify/102-verify_nuget.sh`](100-verify/102-verify_nuget.sh).

```bash
verify_nuget_project "Template"
verify_nuget_project "Another.Library"
```

To build more NuGet packages, add calls like this to [`200-prepare/201-prepare_nuget.sh`](200-prepare/201-prepare_nuget.sh).

```bash
prepare_nuget_package "Template" "$(nuget_artifact_dir "template")"
prepare_nuget_package "Another.Library" "$(nuget_artifact_dir "another-library")"
```

To publish those packages, add matching calls to [`300-publish/302-publish_nuget.sh`](300-publish/302-publish_nuget.sh).

```bash
publish_nuget_packages "$(nuget_artifact_dir "template")"
publish_nuget_packages "$(nuget_artifact_dir "another-library")"
```

Docker image helpers follow the same pattern.

```bash
verify_docker_project "Template.Host"
prepare_docker_image "Template.Host" "owner/template-host"
publish_docker_image "Template.Host" "owner/template-host"
```

Helm helpers accept explicit output directories.

```bash
prepare_helm_chart "${RELEASE_HELM_MANIFEST_DIR}" "${RELEASE_HELM_CHART_DIR}"
publish_helm_charts "${RELEASE_HELM_CHART_DIR}"
```

Keep target-specific logic in the phase-specific `*-common.sh` file when more than one script needs it.
Keep logs on stderr when semantic-release expects stdout to stay parseable.
