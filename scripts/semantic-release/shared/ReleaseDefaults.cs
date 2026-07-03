using System;

namespace Template.Scripting;

/// <summary>
/// Captures the default semantic-release configuration values shared across file-based release scripts.
/// </summary>
/// <remarks>
/// <see cref="Load" /> preserves the release defaults used before the file-based script migration so phase scripts can keep their existing behavior.
/// Relative directory values are intentionally kept as configured strings instead of being normalized to absolute paths.
/// </remarks>
public sealed record class ReleaseDefaults
{
    /// <summary>
    /// Gets the build configuration name used by release scripts.
    /// </summary>
    /// <value>
    /// The configured <c>CONFIGURATION</c> environment value, or <c>Release</c> when it is unset, empty, or whitespace-only.
    /// </value>
    public required string Configuration { get; init; }

    /// <summary>
    /// Gets the container registry host or prefix used by release scripts.
    /// </summary>
    /// <value>
    /// The configured <c>REGISTRY</c> environment value, or <c>ghcr.io</c> when it is unset, empty, or whitespace-only.
    /// </value>
    public required string Registry { get; init; }

    /// <summary>
    /// Gets the base artifacts directory for release outputs.
    /// </summary>
    /// <value>
    /// The configured <c>RELEASE_ARTIFACTS_DIR</c> environment value, or <c>artifacts</c> when it is unset, empty, or whitespace-only.
    /// </value>
    public required string ArtifactsDirectory { get; init; }

    /// <summary>
    /// Gets the directory used for NuGet package artifacts.
    /// </summary>
    /// <value>
    /// The configured <c>RELEASE_NUGET_ARTIFACTS_DIR</c> environment value, or <c>{ArtifactsDirectory}/nuget</c> when it is unset, empty, or whitespace-only.
    /// </value>
    public required string NuGetArtifactsDirectory { get; init; }

    /// <summary>
    /// Gets the directory used for rendered Helm manifests.
    /// </summary>
    /// <value>
    /// The configured <c>RELEASE_HELM_MANIFEST_DIR</c> environment value, or <c>{ArtifactsDirectory}/helm/manifest</c> when it is unset, empty, or whitespace-only.
    /// </value>
    public required string HelmManifestDirectory { get; init; }

    /// <summary>
    /// Gets the directory used for packaged Helm charts.
    /// </summary>
    /// <value>
    /// The configured <c>RELEASE_HELM_CHART_DIR</c> environment value, or <c>{ArtifactsDirectory}/helm/chart</c> when it is unset, empty, or whitespace-only.
    /// </value>
    public required string HelmChartDirectory { get; init; }

    /// <summary>
    /// Gets the GitHub repository owner used by release scripts when it is available.
    /// </summary>
    /// <value>
    /// The configured <c>GITHUB_OWNER</c> environment value, or the first segment of <c>GITHUB_REPOSITORY</c> when <c>GITHUB_OWNER</c> is unset.
    /// <see langword="null" /> when neither source is available.
    /// </value>
    /// <remarks>
    /// This intentionally mirrors the shell helper behavior by deriving the owner from <c>GITHUB_REPOSITORY</c> only when <c>GITHUB_OWNER</c> is absent, not when it is present but empty.
    /// </remarks>
    public required string? GitHubOwner { get; init; }

    /// <summary>
    /// Reads semantic-release defaults from the current process environment.
    /// </summary>
    /// <returns>A populated release-defaults value bag that matches the shell script defaults.</returns>
    /// <remarks>
    /// Directory defaults compose from <c>RELEASE_ARTIFACTS_DIR</c> first so downstream paths remain stable when only the base artifacts directory changes.
    /// </remarks>
    /// <exception cref="ScriptConfigurationException">
    /// Thrown when a required environment lookup helper detects invalid script configuration while resolving defaults.
    /// </exception>
    public static ReleaseDefaults Load()
    {
        var artifactsDirectory = ScriptEnvironment.GetOrDefault("RELEASE_ARTIFACTS_DIR", "artifacts");

        return new ReleaseDefaults
        {
            Configuration = ScriptEnvironment.GetOrDefault("CONFIGURATION", "Release"),
            Registry = ScriptEnvironment.GetOrDefault("REGISTRY", "ghcr.io"),
            ArtifactsDirectory = artifactsDirectory,
            NuGetArtifactsDirectory = ScriptEnvironment.GetOrDefault("RELEASE_NUGET_ARTIFACTS_DIR", $"{artifactsDirectory}/nuget"),
            HelmManifestDirectory = ScriptEnvironment.GetOrDefault("RELEASE_HELM_MANIFEST_DIR", $"{artifactsDirectory}/helm/manifest"),
            HelmChartDirectory = ScriptEnvironment.GetOrDefault("RELEASE_HELM_CHART_DIR", $"{artifactsDirectory}/helm/chart"),
            GitHubOwner = GetGitHubOwner(),
        };
    }

    private static string? GetGitHubOwner()
    {
        var configuredOwner = Environment.GetEnvironmentVariable("GITHUB_OWNER");
        if (configuredOwner is not null)
        {
            return configuredOwner;
        }

        var repository = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
        if (repository is null)
        {
            return null;
        }

        var separatorIndex = repository.IndexOf('/', StringComparison.Ordinal);
        return separatorIndex >= 0 ? repository[..separatorIndex] : repository;
    }
}
