using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Template.Scripting;

/// <summary>
/// Runs semantic-release child scripts in a deterministic order.
/// </summary>
/// <remarks>
/// Child scripts are discovered only in the provided directory and executed through <see cref="NativeCommandRunner" /> so native invocation behavior stays centralized.
/// The helper matches the shell release flow by logging each child script before execution and by treating a non-zero child exit code as a release-target failure.
/// </remarks>
public static class ReleaseSubScripts
{
    /// <summary>
    /// Discovers and executes file-based child scripts whose names start with the provided prefix.
    /// </summary>
    /// <param name="directory">Directory that contains the child scripts.</param>
    /// <param name="prefix">File-name prefix used to select child scripts.</param>
    /// <param name="cancellationToken">Token that cancels child-script execution.</param>
    /// <returns>A task that completes when every matching child script has finished successfully, or when no matching child scripts exist.</returns>
    /// <remarks>
    /// Matching files use the pattern <c>{prefix}*.cs</c> in the top directory only and execute in ordinal file-name order.
    /// Each child script runs as <c>dotnet run --file &lt;script&gt;</c> from the current process working directory with <see cref="NativeCommandSpec.ThrowOnFailure" /> disabled so exit-code handling stays release-specific.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="directory" /> or <paramref name="prefix" /> is empty.</exception>
    /// <exception cref="ReleaseTargetException">
    /// Thrown when <paramref name="directory" /> does not exist, when a child script cannot be started, or when a child script exits with a non-zero exit code.
    /// </exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken" /> cancels child-script execution.</exception>
    public static async Task RunWithPrefixAsync(string directory, string prefix, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new ArgumentException("Child script directory cannot be empty.", nameof(directory));
        }

        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new ArgumentException("Child script prefix cannot be empty.", nameof(prefix));
        }

        if (!Directory.Exists(directory))
        {
            throw new ReleaseTargetException($"Directory '{directory}' does not exist.");
        }

        var scripts = Directory
            .EnumerateFiles(directory, $"{prefix}*.cs", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        if (scripts.Length == 0)
        {
            Console.Error.WriteLine($"no sub-scripts found in {directory} with prefix {prefix}");
            return;
        }

        foreach (var script in scripts)
        {
            Console.Error.WriteLine($"running {script}");

            NativeCommandResult result;
            try
            {
                result = await NativeCommandRunner.RunAsync(
                    new NativeCommandSpec
                    {
                        Executable = "dotnet",
                        Arguments = ["run", "--file", script],
                        WorkingDirectory = Environment.CurrentDirectory,
                        ThrowOnFailure = false,
                    },
                    cancellationToken);
            }
            catch (NativeCommandException ex)
            {
                throw new ReleaseTargetException($"Failed running sub-script '{script}'. Ensure the .NET SDK is installed and available on PATH.", ex);
            }

            if (result.ExitCode != 0)
            {
                throw new ReleaseTargetException($"Failed running sub-script '{script}', exited with {result.ExitCode}.");
            }
        }
    }
}
