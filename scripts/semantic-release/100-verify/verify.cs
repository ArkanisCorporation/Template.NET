#!/usr/bin/env -S dotnet --
#:property TargetFramework=net10.0
#:property Nullable=enable
#:property ManagePackageVersionsCentrally=false
#:property RestorePackagesWithLockFile=false
#:property ExperimentalFileBasedProgramEnableTransitiveDirectives=true
#:include ../../shared/*.cs
#:include ../shared/*.cs
#:include VerifyTargets.cs

using System.IO;
using System.Threading;
using CliWrap;
using Template.Scripting;

using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += OnCancelKeyPress;

try
{
    _ = ScriptEnvironment.Require("VERSION");
    _ = ScriptEnvironment.Require("VERSION_TAG");
    _ = ScriptEnvironment.Require("VERSION_CHANNEL");

    Console.Error.WriteLine("Restoring .NET tools...");

    await NativeCommandRunner.RunAsync(
        new NativeCommandSpec
        {
            Executable = "dotnet",
            Arguments = ["tool", "restore"],
            WorkingDirectory = Directory.GetCurrentDirectory(),
            ConfigureCommand = command => command.WithStandardOutputPipe(PipeTarget.ToDelegate(line => Console.Error.WriteLine(line))),
        },
        cancellationTokenSource.Token);

    await ReleaseSubScripts.RunWithPrefixAsync(VerifyTargets.GetScriptDirectory(), "1", cancellationTokenSource.Token);
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
