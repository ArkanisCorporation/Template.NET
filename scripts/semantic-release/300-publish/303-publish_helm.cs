#!/usr/bin/env -S dotnet --
#:property TargetFramework=net10.0
#:property Nullable=enable
#:property ManagePackageVersionsCentrally=false
#:property RestorePackagesWithLockFile=false
#:property ExperimentalFileBasedProgramEnableTransitiveDirectives=true
#:property AssemblyName=SemanticReleasePublishHelm
#:include ../../shared/*.cs
#:include ../shared/*.cs
#:include PublishTargets.cs

using System.Threading;
using Template.Scripting;

using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += OnCancelKeyPress;

try
{
    // Optional Helm chart publish.
    if (ScriptEnvironment.IsEnabled(Environment.GetEnvironmentVariable("RELEASE_HELM_ENABLED")))
    {
        var defaults = ReleaseDefaults.Load();
        await PublishTargets.PublishHelmChartsAsync(defaults.HelmChartDirectory, defaults, cancellationTokenSource.Token);
    }
    else
    {
        PublishTargets.SkipReleaseTarget("Helm chart publish; RELEASE_HELM_ENABLED is not true");
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
