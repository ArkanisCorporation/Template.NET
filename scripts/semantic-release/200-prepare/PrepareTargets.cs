using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;

namespace Template.Scripting;

/// <summary>
/// Provides artifact-preparation helpers for semantic-release prepare scripts.
/// </summary>
/// <remarks>
/// These helpers preserve the existing shell-script command arguments while routing command output to standard error so semantic-release standard output stays available for parseable data.
/// </remarks>
internal static class PrepareTargets
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
    /// Builds one NuGet package into the provided output directory.
    /// </summary>
    /// <param name="projectName">Project directory and project-file stem under <c>src</c>.</param>
    /// <param name="outputDir">Output directory passed to <c>dotnet pack --output</c>.</param>
    /// <param name="defaults">Release defaults that supply the build configuration.</param>
    /// <param name="cancellationToken">Token that cancels package creation.</param>
    /// <returns>A task that completes when package creation has finished successfully.</returns>
    /// <remarks>
    /// This preserves the shell command <c>dotnet pack &lt;project&gt; --configuration &lt;CONFIGURATION&gt; --output &lt;outputDir&gt; --include-symbols --include-source --no-restore</c>.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="projectName" /> or <paramref name="outputDir" /> is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="defaults" /> is <see langword="null" />.</exception>
    /// <exception cref="ReleaseTargetException">Thrown when the project file does not exist.</exception>
    /// <exception cref="NativeCommandException">Thrown when <c>dotnet pack</c> fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken" /> cancels package creation.</exception>
    internal static async Task PrepareNuGetPackageAsync(string projectName, string outputDir, ReleaseDefaults defaults, CancellationToken cancellationToken = default)
    {
        ValidateProjectName(projectName);

        if (string.IsNullOrWhiteSpace(outputDir))
        {
            throw new ArgumentException("Output directory cannot be empty.", nameof(outputDir));
        }

        ArgumentNullException.ThrowIfNull(defaults);

        var projectFile = Path.Combine(".", "src", projectName, $"{projectName}.csproj");
        ReleaseValidation.RequireFile(projectFile, "NuGet project");

        Console.Error.WriteLine($"Building {projectName} NuGet package...");

        await NativeCommandRunner.RunAsync(
            CreateStderrCommandSpec(
                "dotnet",
                ["pack", projectFile, "--configuration", defaults.Configuration, "--output", outputDir, "--include-symbols", "--include-source", "--no-restore"],
                Environment.CurrentDirectory),
            cancellationToken);

        Console.Error.WriteLine($"Successfully built the {projectName} NuGet package");
    }

    /// <summary>
    /// Builds one Docker image from a project Dockerfile.
    /// </summary>
    /// <param name="projectName">Project directory under <c>src</c> that contains the Dockerfile.</param>
    /// <param name="imageNameBare">Bare image name that will be combined with the configured registry.</param>
    /// <param name="defaults">Release defaults that supply the registry and build configuration.</param>
    /// <param name="cancellationToken">Token that cancels image creation.</param>
    /// <returns>A task that completes when image creation has finished successfully.</returns>
    /// <remarks>
    /// This preserves the shell command <c>docker buildx build --load --cache-from type=gha --cache-from &lt;imageName&gt; --cache-to type=inline,mode=max --tag &lt;imageName&gt; --file &lt;dockerfile&gt; --build-arg BUILD_CONFIGURATION=&lt;CONFIGURATION&gt; .</c>.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="projectName" /> or <paramref name="imageNameBare" /> is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="defaults" /> is <see langword="null" />.</exception>
    /// <exception cref="ReleaseTargetException">Thrown when the Dockerfile does not exist.</exception>
    /// <exception cref="NativeCommandException">Thrown when <c>docker buildx build</c> fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken" /> cancels image creation.</exception>
    internal static async Task PrepareDockerImageAsync(string projectName, string imageNameBare, ReleaseDefaults defaults, CancellationToken cancellationToken = default)
    {
        ValidateProjectName(projectName);

        if (string.IsNullOrWhiteSpace(imageNameBare))
        {
            throw new ArgumentException("Image name cannot be empty.", nameof(imageNameBare));
        }

        ArgumentNullException.ThrowIfNull(defaults);

        var dockerfilePath = Path.Combine(".", "src", projectName, "Dockerfile");
        ReleaseValidation.RequireFile(dockerfilePath, "Dockerfile");

        var imageName = GetDockerImageName(imageNameBare, defaults);

        Console.Error.WriteLine($"Building {imageName} from {dockerfilePath}...");

        await NativeCommandRunner.RunAsync(
            CreateStderrCommandSpec(
                "docker",
                [
                    "buildx",
                    "build",
                    "--load",
                    "--cache-from",
                    "type=gha",
                    "--cache-from",
                    imageName,
                    "--cache-to",
                    "type=inline,mode=max",
                    "--tag",
                    imageName,
                    "--file",
                    dockerfilePath,
                    "--build-arg",
                    $"BUILD_CONFIGURATION={defaults.Configuration}",
                    ".",
                ],
                Environment.CurrentDirectory),
            cancellationToken);

        Console.Error.WriteLine($"Successfully built {imageName}");
    }

    /// <summary>
    /// Builds, lints, and packages one Helm chart.
    /// </summary>
    /// <param name="manifestDir">Directory that receives <c>dotnet aspire publish</c> output.</param>
    /// <param name="chartDir">Directory that receives packaged chart archives.</param>
    /// <param name="cancellationToken">Token that cancels chart preparation.</param>
    /// <returns>A task that completes when chart preparation has finished successfully.</returns>
    /// <remarks>
    /// This preserves the shell command sequence <c>dotnet aspire publish --output-path &lt;manifestDir&gt; --environment Kubernetes</c>, <c>helm lint &lt;manifestDir&gt;</c>, and <c>helm package &lt;manifestDir&gt; --destination &lt;chartDir&gt;</c>.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="manifestDir" /> or <paramref name="chartDir" /> is empty.</exception>
    /// <exception cref="NativeCommandException">Thrown when any chart-preparation command fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken" /> cancels chart preparation.</exception>
    internal static async Task PrepareHelmChartAsync(string manifestDir, string chartDir, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(manifestDir))
        {
            throw new ArgumentException("Manifest directory cannot be empty.", nameof(manifestDir));
        }

        if (string.IsNullOrWhiteSpace(chartDir))
        {
            throw new ArgumentException("Chart directory cannot be empty.", nameof(chartDir));
        }

        Console.Error.WriteLine("Building the Helm chart manifest...");

        await NativeCommandRunner.RunAsync(
            CreateStderrCommandSpec(
                "dotnet",
                ["aspire", "publish", "--output-path", manifestDir, "--environment", "Kubernetes"],
                Environment.CurrentDirectory),
            cancellationToken);

        Console.Error.WriteLine("Verifying the Helm chart manifest...");

        await NativeCommandRunner.RunAsync(
            CreateStderrCommandSpec(
                "helm",
                ["lint", manifestDir],
                Environment.CurrentDirectory),
            cancellationToken);

        Console.Error.WriteLine("Packaging the Helm chart...");

        await NativeCommandRunner.RunAsync(
            CreateStderrCommandSpec(
                "helm",
                ["package", manifestDir, "--destination", chartDir],
                Environment.CurrentDirectory),
            cancellationToken);

        Console.Error.WriteLine("Successfully prepared and verified the Helm chart");
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
