#!/usr/bin/env -S dotnet --
#:property TargetFramework=net10.0
#:property Nullable=enable
#:property ManagePackageVersionsCentrally=false
#:property RestorePackagesWithLockFile=false
#:property ExperimentalFileBasedProgramEnableTransitiveDirectives=true
#:property AssemblyName=SemanticReleasePrepareHelm
#:include ../../shared/*.cs
#:include ../shared/*.cs
#:include PrepareTargets.cs

using System.Threading;
using Template.Scripting;

using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += OnCancelKeyPress;

try
{
    // Optional Helm chart artifact creation.
    if (ScriptEnvironment.IsEnabled(Environment.GetEnvironmentVariable("RELEASE_HELM_ENABLED")))
    {
        var defaults = ReleaseDefaults.Load();
        await PrepareTargets.PrepareHelmChartAsync(defaults.HelmManifestDirectory, defaults.HelmChartDirectory, cancellationTokenSource.Token);
    }
    else
    {
        PrepareTargets.SkipReleaseTarget("Helm chart build; RELEASE_HELM_ENABLED is not true");
    }

    return 0;
}
catch (ScriptException exception)
{
    Console.Error.WriteLine(exception.Message);
    return exception.ExitCode;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 2;
}
finally
{
    Console.CancelKeyPress -= OnCancelKeyPress;
}

void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs eventArgs)
{
    eventArgs.Cancel = true;
    cancellationTokenSource.Cancel();
}
