using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;

namespace Template.Scripting;

/// <summary>
/// Provides release-target verification helpers for semantic-release verify scripts.
/// </summary>
/// <remarks>
/// These helpers preserve the existing shell-script contract by checking tool availability and target files without creating release artifacts.
/// Command probes route standard output to standard error so semantic-release verify output stays parseable.
/// </remarks>
internal static class VerifyTargets
{
    /// <summary>
    /// Gets the directory that contains the calling file-based script.
    /// </summary>
    /// <param name="filePath">Compiler-supplied path for the calling script file.</param>
    /// <returns>The absolute directory path that contains the calling script.</returns>
    /// <remarks>
    /// File-based scripts compile into a temporary output directory at runtime, so they cannot use <see cref="AppContext.BaseDirectory" /> to discover sibling script files.
    /// This helper keeps phase entry points anchored to their source-script directory.
    /// </remarks>
    /// <exception cref="ScriptConfigurationException">Thrown when the calling file path does not contain a parent directory.</exception>
    internal static string GetScriptDirectory([CallerFilePath] string filePath = "")
        => Path.GetDirectoryName(filePath)
            ?? throw new ScriptConfigurationException($"Failed to resolve a script directory from '{filePath}'.");

    /// <summary>
    /// Verifies that a NuGet release project exists and that the .NET SDK is available.
    /// </summary>
    /// <param name="projectName">Project directory and project-file stem under <c>src</c>.</param>
    /// <param name="cancellationToken">Token that cancels command validation.</param>
    /// <returns>A task that completes when the project has been verified successfully.</returns>
    /// <remarks>
    /// The project file path matches the shell convention <c>./src/&lt;projectName&gt;/&lt;projectName&gt;.csproj</c>.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="projectName" /> is empty.</exception>
    /// <exception cref="ReleaseTargetException">Thrown when the .NET SDK is unavailable or when the project file does not exist.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken" /> cancels verification.</exception>
    internal static async Task VerifyNuGetProjectAsync(string projectName, CancellationToken cancellationToken = default)
    {
        ValidateProjectName(projectName);

        await RequireCommandAsync("dotnet", cancellationToken);

        var projectFile = Path.Combine(".", "src", projectName, $"{projectName}.csproj");
        ReleaseValidation.RequireFile(projectFile, "NuGet project");

        Console.Error.WriteLine($"Verified NuGet release target {projectName}");
    }

    /// <summary>
    /// Verifies that a Docker release project exists and that Docker is available.
    /// </summary>
    /// <param name="projectName">Project directory under <c>src</c> that contains the Dockerfile.</param>
    /// <param name="cancellationToken">Token that cancels command validation.</param>
    /// <returns>A task that completes when the project has been verified successfully.</returns>
    /// <remarks>
    /// The Dockerfile path matches the shell convention <c>./src/&lt;projectName&gt;/Dockerfile</c>.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="projectName" /> is empty.</exception>
    /// <exception cref="ReleaseTargetException">Thrown when Docker is unavailable or when the Dockerfile does not exist.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken" /> cancels verification.</exception>
    internal static async Task VerifyDockerProjectAsync(string projectName, CancellationToken cancellationToken = default)
    {
        ValidateProjectName(projectName);

        await RequireCommandAsync("docker", cancellationToken);

        var dockerfilePath = Path.Combine(".", "src", projectName, "Dockerfile");
        ReleaseValidation.RequireFile(dockerfilePath, "Dockerfile");

        Console.Error.WriteLine($"Verified Docker release target {projectName}");
    }

    /// <summary>
    /// Verifies that the optional Helm release target can run.
    /// </summary>
    /// <param name="cancellationToken">Token that cancels command validation.</param>
    /// <returns>A task that completes when the Helm release target has been verified successfully.</returns>
    /// <remarks>
    /// This preserves the shell behavior by requiring both <c>dotnet</c> and <c>helm</c>, then ensuring a GitHub owner is configured or derivable from repository environment variables.
    /// </remarks>
    /// <exception cref="ReleaseTargetException">Thrown when a required command is unavailable.</exception>
    /// <exception cref="ScriptConfigurationException">Thrown when the GitHub owner cannot be resolved.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken" /> cancels verification.</exception>
    internal static async Task VerifyHelmReleaseTargetAsync(CancellationToken cancellationToken = default)
    {
        await RequireCommandAsync("dotnet", cancellationToken);
        await RequireCommandAsync("helm", cancellationToken);

        var defaults = ReleaseDefaults.Load();
        if (string.IsNullOrWhiteSpace(defaults.GitHubOwner))
        {
            throw new ScriptConfigurationException("Environment variable 'GITHUB_OWNER' is not set or is empty. Set 'GITHUB_OWNER' or 'GITHUB_REPOSITORY' before running this repository automation script.");
        }

        Console.Error.WriteLine("Verified Helm release target");
    }

    /// <summary>
    /// Writes a standardized skip message for an optional release target.
    /// </summary>
    /// <param name="message">Human-readable explanation for the skipped target.</param>
    /// <remarks>
    /// Standardized skip messages keep CI logs easy to scan while preserving the shell-script phrasing.
    /// </remarks>
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

    private static async Task RequireCommandAsync(string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Command name cannot be empty.", nameof(name));
        }

        try
        {
            var result = await NativeCommandRunner.RunAsync(
                new NativeCommandSpec
                {
                    Executable = name,
                    Arguments = GetPresenceProbeArguments(name),
                    WorkingDirectory = Environment.CurrentDirectory,
                    ThrowOnFailure = false,
                    ConfigureCommand = command => command.WithStandardOutputPipe(PipeTarget.ToDelegate(line => Console.Error.WriteLine(line))),
                },
                cancellationToken);

            if (result.ExitCode != 0)
            {
                throw new ReleaseTargetException(
                    $"Required command '{name}' exited with code {result.ExitCode}. Install '{name}' and ensure it is available on PATH before running this release target.");
            }
        }
        catch (NativeCommandException ex)
        {
            throw new ReleaseTargetException(
                $"Required command '{name}' is not available. Install '{name}' and ensure it is available on PATH before running this release target.",
                ex);
        }
    }

    private static IReadOnlyList<string> GetPresenceProbeArguments(string name)
        => string.Equals(name, "helm", StringComparison.Ordinal)
            ? ["version"]
            : ["--version"];
}
