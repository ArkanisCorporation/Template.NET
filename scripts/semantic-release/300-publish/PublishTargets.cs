using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;

namespace Template.Scripting;

/// <summary>
/// Provides artifact-publishing helpers for semantic-release publish scripts.
/// </summary>
/// <remarks>
/// These helpers preserve the existing shell-script publish arguments, tag rules, and skip behavior while routing command output to standard error so semantic-release standard output remains available for parseable release metadata.
/// </remarks>
internal static class PublishTargets
{
    /// <summary>
    /// Gets the directory that contains the calling file-based script.
    /// </summary>
    /// <param name="filePath">Compiler-supplied path for the calling script file.</param>
    /// <returns>The absolute directory path that contains the calling script.</returns>
    /// <exception cref="ScriptConfigurationException">Thrown when the calling file path does not contain a parent directory.</exception>
    internal static string GetScriptDirectory([CallerFilePath] string filePath = "")
        => Path.GetDirectoryName(filePath)
            ?? throw new ScriptConfigurationException($"Failed to resolve a script directory from '{filePath}'.");

    /// <summary>
    /// Publishes all NuGet packages from one source directory.
    /// </summary>
    /// <param name="sourceDir">Directory that contains <c>.nupkg</c> files to publish.</param>
    /// <param name="cancellationToken">Token that cancels package publishing.</param>
    /// <returns>A task that completes when every package has been published successfully.</returns>
    /// <remarks>
    /// This preserves the shell command <c>dotnet nuget push &lt;package&gt; --source &lt;url&gt; --api-key &lt;key&gt; --skip-duplicate</c> and the default publish source <c>https://api.nuget.org/v3/index.json</c>.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sourceDir" /> is empty.</exception>
    /// <exception cref="ScriptConfigurationException">Thrown when the NuGet API key is missing.</exception>
    /// <exception cref="ReleaseTargetException">Thrown when no packages are found.</exception>
    /// <exception cref="NativeCommandException">Thrown when any <c>dotnet nuget push</c> command fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken" /> cancels package publishing.</exception>
    internal static async Task PublishNuGetPackagesAsync(string sourceDir, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceDir))
        {
            throw new ArgumentException("Source directory cannot be empty.", nameof(sourceDir));
        }

        var apiKey = ScriptEnvironment.Require("NUGET_PUBLISH_API_KEY");
        var sourceUrl = ScriptEnvironment.GetOrDefault("NUGET_PUBLISH_SOURCE_URL", "https://api.nuget.org/v3/index.json");

        if (!Directory.Exists(sourceDir))
        {
            throw new ReleaseTargetException($"No NuGet packages found in {sourceDir}");
        }

        var packages = Directory.EnumerateFiles(sourceDir, "*.nupkg", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        if (packages.Length == 0)
        {
            throw new ReleaseTargetException($"No NuGet packages found in {sourceDir}");
        }

        foreach (var package in packages)
        {
            Console.Error.WriteLine($"Pushing {package} to {sourceUrl}...");

            await NativeCommandRunner.RunAsync(
                CreateStderrCommandSpec(
                    "dotnet",
                    ["nuget", "push", package, "--source", sourceUrl, "--api-key", apiKey, "--skip-duplicate"],
                    Environment.CurrentDirectory),
                cancellationToken);
        }

        Console.Error.WriteLine($"Successfully published {packages.Length} NuGet package(s)");
    }

    /// <summary>
    /// Publishes one Docker image from a project Dockerfile.
    /// </summary>
    /// <param name="projectName">Project directory under <c>src</c> that contains the Dockerfile.</param>
    /// <param name="imageNameBare">Bare image name that will be combined with the configured registry.</param>
    /// <param name="defaults">Release defaults that supply the registry and build configuration.</param>
    /// <param name="cancellationToken">Token that cancels image publishing.</param>
    /// <returns>A task that completes when image publishing has finished successfully.</returns>
    /// <remarks>
    /// This preserves the shell tag behavior by publishing the version tag, the <c>&lt;channel&gt;-latest</c> tag, and the plain <c>latest</c> tag only for the <c>stable</c> channel.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="projectName" /> or <paramref name="imageNameBare" /> is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="defaults" /> is <see langword="null" />.</exception>
    /// <exception cref="ScriptConfigurationException">Thrown when required semantic-release environment values are missing.</exception>
    /// <exception cref="ReleaseTargetException">Thrown when the Dockerfile does not exist.</exception>
    /// <exception cref="NativeCommandException">Thrown when any <c>docker buildx build</c> command fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken" /> cancels image publishing.</exception>
    internal static async Task PublishDockerImageAsync(string projectName, string imageNameBare, ReleaseDefaults defaults, CancellationToken cancellationToken = default)
    {
        ValidateProjectName(projectName);

        if (string.IsNullOrWhiteSpace(imageNameBare))
        {
            throw new ArgumentException("Image name cannot be empty.", nameof(imageNameBare));
        }

        ArgumentNullException.ThrowIfNull(defaults);

        var versionTag = ScriptEnvironment.Require("VERSION_TAG");
        var versionChannel = ScriptEnvironment.Require("VERSION_CHANNEL");
        var dockerfilePath = Path.Combine(".", "src", projectName, "Dockerfile");
        ReleaseValidation.RequireFile(dockerfilePath, "Dockerfile");

        var imageName = GetDockerImageName(imageNameBare, defaults);
        var imageNameVersionTagged = $"{imageName}:{versionTag}";
        var imageNameChannelTagged = $"{imageName}:{versionChannel}-latest";
        var imageNameLatestTagged = $"{imageName}:latest";

        Console.Error.WriteLine($"Publishing {imageName} from {dockerfilePath}...");
        Console.Error.WriteLine($"  as {imageNameVersionTagged}");
        Console.Error.WriteLine($"  as {imageNameChannelTagged}");

        await NativeCommandRunner.RunAsync(
            CreateStderrCommandSpec(
                "docker",
                [
                    "buildx",
                    "build",
                    "--push",
                    "--cache-to",
                    "type=gha",
                    "--tag",
                    imageNameVersionTagged,
                    "--tag",
                    imageNameChannelTagged,
                    "--file",
                    dockerfilePath,
                    "--build-arg",
                    $"BUILD_CONFIGURATION={defaults.Configuration}",
                    ".",
                ],
                Environment.CurrentDirectory),
            cancellationToken);

        if (string.Equals(versionChannel, "stable", StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"  as {imageNameLatestTagged}");

            await NativeCommandRunner.RunAsync(
                CreateStderrCommandSpec(
                    "docker",
                    [
                        "buildx",
                        "build",
                        "--push",
                        "--tag",
                        imageNameLatestTagged,
                        "--file",
                        dockerfilePath,
                        "--build-arg",
                        $"BUILD_CONFIGURATION={defaults.Configuration}",
                        ".",
                    ],
                    Environment.CurrentDirectory),
                cancellationToken);
        }

        Console.Error.WriteLine($"Successfully published {imageName}");
    }

    /// <summary>
    /// Publishes all Helm chart packages from one chart directory.
    /// </summary>
    /// <param name="chartDir">Directory that contains chart archives to publish.</param>
    /// <param name="defaults">Release defaults that supply the GitHub owner.</param>
    /// <param name="cancellationToken">Token that cancels chart publishing.</param>
    /// <returns>A task that completes when every chart has been published successfully.</returns>
    /// <remarks>
    /// This preserves the shell destination format <c>oci://ghcr.io/&lt;owner&gt;/charts</c> with the owner lowercased using invariant rules.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="chartDir" /> is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="defaults" /> is <see langword="null" />.</exception>
    /// <exception cref="ScriptConfigurationException">Thrown when the GitHub owner cannot be resolved.</exception>
    /// <exception cref="ReleaseTargetException">Thrown when no chart archives are found.</exception>
    /// <exception cref="NativeCommandException">Thrown when any <c>helm push</c> command fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken" /> cancels chart publishing.</exception>
    internal static async Task PublishHelmChartsAsync(string chartDir, ReleaseDefaults defaults, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(chartDir))
        {
            throw new ArgumentException("Chart directory cannot be empty.", nameof(chartDir));
        }

        ArgumentNullException.ThrowIfNull(defaults);

        if (string.IsNullOrWhiteSpace(defaults.GitHubOwner))
        {
            throw new ScriptConfigurationException("Environment variable 'GITHUB_OWNER' is not set or is empty. Set 'GITHUB_OWNER' or 'GITHUB_REPOSITORY' before running this repository automation script.");
        }

        var githubOwnerBare = ScriptEnvironment.LowerInvariant(defaults.GitHubOwner);

        if (!Directory.Exists(chartDir))
        {
            throw new ReleaseTargetException($"No Helm charts found in {chartDir}");
        }

        var charts = Directory.EnumerateFiles(chartDir, "*.tgz", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        if (charts.Length == 0)
        {
            throw new ReleaseTargetException($"No Helm charts found in {chartDir}");
        }

        foreach (var chart in charts)
        {
            Console.Error.WriteLine($"Publishing {chart} Helm chart...");

            await NativeCommandRunner.RunAsync(
                CreateStderrCommandSpec(
                    "helm",
                    ["push", chart, $"oci://ghcr.io/{githubOwnerBare}/charts"],
                    Environment.CurrentDirectory),
                cancellationToken);
        }

        Console.Error.WriteLine($"Successfully published {charts.Length} Helm chart(s)");
    }

    /// <summary>
    /// Writes a standardized skip message for an optional release target.
    /// </summary>
    /// <param name="message">Human-readable explanation for the skipped target.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="message" /> is empty.</exception>
    internal static void SkipReleaseTarget(string message)
        => ReleaseValidation.SkipReleaseTarget(message);

    private static void ValidateProjectName(string projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            throw new ArgumentException("Project name cannot be empty.", nameof(projectName));
        }
    }

    private static string GetDockerImageName(string imageNameBare, ReleaseDefaults defaults)
        => $"{defaults.Registry}/{ScriptEnvironment.LowerInvariant(imageNameBare)}";

    private static NativeCommandSpec CreateStderrCommandSpec(string executable, IReadOnlyList<string> arguments, string workingDirectory)
        => new()
        {
            Executable = executable,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            ConfigureCommand = command => command.WithStandardOutputPipe(PipeTarget.ToDelegate(line => Console.Error.WriteLine(line))),
        };
}
