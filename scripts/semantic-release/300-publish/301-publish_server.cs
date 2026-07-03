#!/usr/bin/env -S dotnet --
#:property TargetFramework=net10.0
#:property Nullable=enable
#:property ManagePackageVersionsCentrally=false
#:property RestorePackagesWithLockFile=false
#:property ExperimentalFileBasedProgramEnableTransitiveDirectives=true
#:property AssemblyName=SemanticReleasePublishServer
#:include ../../shared/*.cs
#:include ../shared/*.cs
#:include PublishTargets.cs

using System.Threading;
using Template.Scripting;

using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += OnCancelKeyPress;

try
{
    // Optional server container publish.
    // var defaults = ReleaseDefaults.Load();
    // await PublishTargets.PublishDockerImageAsync("Template", ScriptEnvironment.LowerInvariant(ScriptEnvironment.Require("GITHUB_REPOSITORY")), defaults, cancellationTokenSource.Token);
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
