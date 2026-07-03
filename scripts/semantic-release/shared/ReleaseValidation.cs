using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Template.Scripting;

/// <summary>
/// Validates semantic-release prerequisites and target inputs.
/// </summary>
/// <remarks>
/// These helpers convert infrastructure checks into release-specific diagnostics so phase scripts can report a consistent error surface to CI and human operators.
/// </remarks>
public static class ReleaseValidation
{
    /// <summary>
    /// Verifies that a required command is available by executing its version command.
    /// </summary>
    /// <param name="name">Executable name available on <c>PATH</c>.</param>
    /// <param name="cancellationToken">Token that cancels command validation.</param>
    /// <returns>A task that completes when the command exits successfully.</returns>
    /// <remarks>
    /// The validation executes <c>{name} --version</c> through <see cref="NativeCommandRunner" /> with <see cref="NativeCommandSpec.ThrowOnFailure" /> disabled so the release layer can provide installation and <c>PATH</c> remediation.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name" /> is empty.</exception>
    /// <exception cref="ReleaseTargetException">Thrown when the command cannot be started or exits with a non-zero exit code.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken" /> cancels command validation.</exception>
    public static async Task RequireCommandAsync(string name, CancellationToken cancellationToken = default)
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
                    Arguments = ["--version"],
                    WorkingDirectory = Environment.CurrentDirectory,
                    ThrowOnFailure = false,
                },
                cancellationToken);

            if (result.ExitCode != 0)
            {
                throw CreateMissingCommandException(name, result.ExitCode);
            }
        }
        catch (NativeCommandException ex)
        {
            throw new ReleaseTargetException(
                $"Required command '{name}' is not available. Install '{name}' and ensure it is available on PATH before running this release target.",
                ex);
        }
    }

    /// <summary>
    /// Verifies that a required file exists.
    /// </summary>
    /// <param name="path">File path to validate.</param>
    /// <param name="targetKind">Human-readable release target or artifact description.</param>
    /// <remarks>
    /// Use <paramref name="targetKind" /> to make the failure message actionable for the calling release phase.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path" /> or <paramref name="targetKind" /> is empty.</exception>
    /// <exception cref="ReleaseTargetException">Thrown when the file does not exist.</exception>
    public static void RequireFile(string path, string targetKind)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Required file path cannot be empty.", nameof(path));
        }

        if (string.IsNullOrWhiteSpace(targetKind))
        {
            throw new ArgumentException("Target kind cannot be empty.", nameof(targetKind));
        }

        if (!File.Exists(path))
        {
            throw new ReleaseTargetException($"Required {targetKind} file '{path}' does not exist.");
        }
    }

    /// <summary>
    /// Verifies that a required directory exists.
    /// </summary>
    /// <param name="path">Directory path to validate.</param>
    /// <param name="targetKind">Human-readable release target or artifact description.</param>
    /// <remarks>
    /// Use <paramref name="targetKind" /> to make the failure message actionable for the calling release phase.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path" /> or <paramref name="targetKind" /> is empty.</exception>
    /// <exception cref="ReleaseTargetException">Thrown when the directory does not exist.</exception>
    public static void RequireDirectory(string path, string targetKind)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Required directory path cannot be empty.", nameof(path));
        }

        if (string.IsNullOrWhiteSpace(targetKind))
        {
            throw new ArgumentException("Target kind cannot be empty.", nameof(targetKind));
        }

        if (!Directory.Exists(path))
        {
            throw new ReleaseTargetException($"Required {targetKind} directory '{path}' does not exist.");
        }
    }

    /// <summary>
    /// Writes a standardized skip message for a release target.
    /// </summary>
    /// <param name="message">Human-readable explanation for the skipped target.</param>
    /// <remarks>
    /// This helper writes to standard error so skip diagnostics stay visible in CI logs even when standard output is redirected.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="message" /> is empty.</exception>
    public static void SkipReleaseTarget(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Skip message cannot be empty.", nameof(message));
        }

        Console.Error.WriteLine($"Skipping {message}");
    }

    private static ReleaseTargetException CreateMissingCommandException(string name, int exitCode)
        => new($"Required command '{name}' exited with code {exitCode}. Install '{name}' and ensure it is available on PATH before running this release target.");
}
