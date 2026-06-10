# GitHub Actions

This repository keeps human-triggered and automated CI/CD workflows in `.github/workflows`.
Reusable workflows live directly in `.github/workflows` because GitHub does not support reusable workflow files in subdirectories.

## Workflow Map

`main.yaml` is the normal CI/CD entry point.
It runs tests, runs semantic-release, and optionally deploys staging Kubernetes resources after a release.
It runs on pushes to `main`, `release/*`, and `ci`, pull requests targeting `main`, manual dispatches, and repository dispatches.

`_test.yaml` is the reusable .NET test workflow.
It restores local .NET tools, restores NuGet dependencies in locked mode, builds the solution, and runs tests.
It accepts an optional test filter and runner override.

`_release.yaml` is the reusable semantic-release workflow.
It restores dependencies, prepares Docker Buildx, logs in to GitHub Container Registry, installs Kubernetes tooling, and runs semantic-release.
It uses dry run mode for pull requests and the `ci` branch.
It can create a release backpropagation pull request after a real release on `release/stable`.

`deploy-kubernetes.yaml` is the manual and callable deployment entry point.
It maps the public environment choice to the internal Aspire environment and Kubernetes namespace.
It supports `staging` and `production` environments.

`_deploy-kubernetes.yaml` is the reusable deployment workflow.
It applies the Kubernetes namespace and runs `dotnet tool run aspire -- deploy`.
It runs on `arkanis-runners` and binds the job to the requested GitHub environment.

## Runner Trust

Use `ubuntu-latest` for untrusted pull request validation.
Use `arkanis-runners` only for trusted pushes, trusted manual runs, release publishing, and Kubernetes deployment.
Public downstream repositories must not run untrusted pull request code on persistent self-hosted runners.
The `runner` workflow input exists on test and release workflows so maintainers can fall back to `ubuntu-latest` when the self-hosted pool is unavailable.
Deployment remains fixed to `arkanis-runners` because it depends on trusted runner access to Kubernetes deployment infrastructure.

## Token And Secrets

Default workflow permissions should be read-only.
Grant write permissions only on jobs that publish releases, packages, comments, backpropagation pull requests, or deployments.
The top-level CI/CD workflow currently grants `contents: write`, `issues: write`, `pull-requests: write`, and `packages: write` because the release workflow publishes GitHub releases, comments on released work, creates release backpropagation pull requests, and publishes packages.
Deployment workflows use `contents: read` and `packages: read`.
Prefer named reusable workflow secrets over `secrets: inherit` for jobs that need only a small set of secrets.
`GITHUB_TOKEN` is used for GitHub release and GHCR operations.
`PR_AUTOMATION_PAT` is required only for automated approval of release backpropagation pull requests.
Do not store real secrets in committed files.
Use `.act/secrets` or secure interactive prompts for local-only `act` secrets.

## Tool Versions

The .NET SDK version is selected by `global.json`.
The Aspire, Husky, and setversion tool versions are selected by `dotnet-tools.json`.
GitHub Actions versions are pinned in workflow YAML.
Keep action versions explicit in the workflow files so reviews and dependency updates can see each tool boundary.
kubectl and Helm use the action defaults unless explicit versions are added.
Update those defaults deliberately when the target Kubernetes clusters and chart toolchain have been verified.

## Dependency Update Bot

This template uses Renovate as the dependency-update bot.
Do not add Dependabot for the same ecosystems unless Renovate is removed first.
Duplicate update bots create noisy and conflicting pull requests.
Renovate should cover GitHub Actions, .NET dependencies, local .NET tools, semantic-release npm packages, Kubernetes and Helm workflow pins, and lock-file maintenance.
Review major updates manually because GitHub Actions, Helm, Kubernetes, and semantic-release major versions can change runtime behavior.

## Local Validation

Use `act` for local pull request and CI branch test workflow validation.
Use the commands in the README `nektos/act` section as the detailed local validation source.
Keep those README commands current when wrapper scripts, runner mappings, or supported local validation targets change.
Use GitHub-hosted Actions for final release and deployment validation because local `act` runs do not fully emulate GitHub environments, permissions, registry credentials, Kubernetes credentials, release state, or concurrency.

## Release Behavior

Pull requests and the `ci` branch run semantic-release in dry run mode.
Trusted branch pushes can publish releases when semantic-release detects a new version from commit history.
The release workflow publishes to GitHub and GitHub Container Registry with `GITHUB_TOKEN`.
Real releases on `release/stable` can create and auto-merge a backpropagation pull request to the default branch.

## Deployment Behavior

Staging deployment can run automatically from `main` after a release publishes a new version.
Maintainers can also force staging deployment with the `deploy_staging` manual workflow input.
Manual Kubernetes deployment supports `staging` and `production`.
Deployment jobs use GitHub environments, the configured Kubernetes namespace, and Aspire environment names.
The optional `image_tag` input overrides `Kubernetes:Images:Tag` during deployment.

## Repository Settings Checklist

Require the `Tests` check before merging to the default branch.
Add any workflow quality check to the required status checks when that workflow exists.
Protect `release/stable` with required review and required CI checks before using it for production releases.
Configure staging and production GitHub environments with reviewers and deployment branch restrictions.
Restrict self-hosted runner groups to repositories and workflows that are allowed to use them.
Prefer OIDC and short-lived cloud credentials for future external cloud authentication.
Keep Renovate enabled for GitHub Actions, NuGet, npm, and Docker-related updates.
