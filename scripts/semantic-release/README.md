# Semantic Release Scripts

This directory contains C# file-based apps used by [`@semantic-release/exec`](https://github.com/semantic-release/exec).
The scripts are split by semantic-release lifecycle phase.
Run them with `dotnet run --file`.

## Entry Points

[`release.config.mjs`](../../release.config.mjs) calls these entry points.

- [`100-verify/verify.cs`](100-verify/verify.cs) runs `verifyReleaseCmd`.
- [`200-prepare/prepare.cs`](200-prepare/prepare.cs) runs `prepareCmd`.
- [`300-publish/publish.cs`](300-publish/publish.cs) runs `publishCmd`.

Each executable entry starts with `#!/usr/bin/env -S dotnet --`.
Each executable entry then sets the file-based app properties used by this repository, including `RestorePackagesWithLockFile=false`.
Shared script package references live in [`../shared/Packages.cs`](../shared/Packages.cs).
Shared script logic is included with `#:include`.

## Script Ordering

Sub-scripts are selected by filename prefix.
`ReleaseSubScripts.RunWithPrefixAsync` finds matching `*.cs` files directly in the provided directory, sorts them by filename, and runs them in order with `dotnet run --file`.

Current prefixes are:

- `1` for verify sub-scripts.
- `2` for prepare sub-scripts.
- `3` for publish sub-scripts.

For example, `300-publish/publish.cs` runs `301-publish_server.cs`, `302-publish_nuget.cs`, and `303-publish_helm.cs` in that order.

## Phase Responsibilities

The verify phase checks that configured targets and required tools are available.
It should not create release artifacts.

The prepare phase applies the release version and creates artifacts.
It runs `dotnet setversion --recursive "$VERSION"` before running prepare sub-scripts.

The publish phase publishes artifacts created during prepare.
It should consume existing artifacts rather than creating new ones.

Keep logs on stderr when semantic-release expects stdout to stay parseable.
The shared helpers route native command output through `CliWrap` and mirror release command logs to stderr.

## Defaults

Shared defaults are defined in [`shared/ReleaseDefaults.cs`](shared/ReleaseDefaults.cs).
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
When `GITHUB_REPOSITORY` is available, `ReleaseDefaults` derives `GITHUB_OWNER` from it.

Docker image helpers accept an image name argument.
The template leaf scripts keep the default Docker and NuGet target calls commented until a downstream project opts into them.

The template leaf scripts use `RELEASE_HELM_ENABLED=true` as an optional switch for Helm targets.
When it is not enabled, the default Helm verify, prepare, and publish scripts skip Helm work.

## Manual Runs

Restore local tools before running the release scripts manually.

```powershell
dotnet tool restore
```

Run the verify phase with sample semantic-release values.

```powershell
$env:VERSION = "1.2.3"
$env:VERSION_TAG = "v1.2.3"
$env:VERSION_CHANNEL = "ci"
dotnet run --file scripts/semantic-release/100-verify/verify.cs
```

Run the prepare phase with sample semantic-release values.

```powershell
$env:VERSION = "1.2.3"
$env:VERSION_TAG = "v1.2.3"
$env:VERSION_CHANNEL = "ci"
dotnet run --file scripts/semantic-release/200-prepare/prepare.cs
```

Run the publish phase only after prepare has created artifacts.

```powershell
$env:VERSION = "1.2.3"
$env:VERSION_TAG = "v1.2.3"
$env:VERSION_CHANNEL = "ci"
$env:NUGET_PUBLISH_API_KEY = "example"
dotnet run --file scripts/semantic-release/300-publish/publish.cs
```

Use a non-production NuGet source when testing publish locally.

```powershell
$env:NUGET_PUBLISH_SOURCE_URL = "https://apiint.nugettest.org/v3/index.json"
```

## Adding Release Targets

Add a verify script when a target has prerequisites that can fail early.
Name it with a `1` prefix.

Add a prepare script when a target creates artifacts.
Name it with a `2` prefix.

Add a publish script when a target uploads artifacts.
Name it with a `3` prefix.

The shared helper methods are reusable and accept the target as an argument.
This keeps each leaf script free to handle one or more projects explicitly.

To verify more NuGet projects, add calls like this to [`100-verify/102-verify_nuget.cs`](100-verify/102-verify_nuget.cs).

```csharp
await VerifyTargets.VerifyNuGetProjectAsync("Template", cancellationTokenSource.Token);
await VerifyTargets.VerifyNuGetProjectAsync("Another.Library", cancellationTokenSource.Token);
```

To build more NuGet packages, add calls like this to [`200-prepare/202-prepare_nuget.cs`](200-prepare/202-prepare_nuget.cs).

```csharp
var defaults = ReleaseDefaults.Load();
await PrepareTargets.PrepareNuGetPackageAsync("Template", Path.Combine(defaults.NuGetArtifactsDirectory, "template"), defaults, cancellationTokenSource.Token);
await PrepareTargets.PrepareNuGetPackageAsync("Another.Library", Path.Combine(defaults.NuGetArtifactsDirectory, "another-library"), defaults, cancellationTokenSource.Token);
```

To publish those packages, add matching calls to [`300-publish/302-publish_nuget.cs`](300-publish/302-publish_nuget.cs).

```csharp
var defaults = ReleaseDefaults.Load();
await PublishTargets.PublishNuGetPackagesAsync(Path.Combine(defaults.NuGetArtifactsDirectory, "template"), cancellationTokenSource.Token);
await PublishTargets.PublishNuGetPackagesAsync(Path.Combine(defaults.NuGetArtifactsDirectory, "another-library"), cancellationTokenSource.Token);
```

Docker image helpers follow the same pattern.

```csharp
await VerifyTargets.VerifyDockerProjectAsync("Template.Host", cancellationTokenSource.Token);
await PrepareTargets.PrepareDockerImageAsync("Template.Host", "owner/template-host", defaults, cancellationTokenSource.Token);
await PublishTargets.PublishDockerImageAsync("Template.Host", "owner/template-host", defaults, cancellationTokenSource.Token);
```

Helm helpers accept explicit output directories.

```csharp
await PrepareTargets.PrepareHelmChartAsync(defaults.HelmManifestDirectory, defaults.HelmChartDirectory, cancellationTokenSource.Token);
await PublishTargets.PublishHelmChartsAsync(defaults.HelmChartDirectory, defaults, cancellationTokenSource.Token);
```
