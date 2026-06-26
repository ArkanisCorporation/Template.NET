# Template.NET

This repository is a GitHub template for .NET projects.
It documents only the tools that a downstream project can run manually.

## Setup

Use the .NET SDK selected by [`global.json`](global.json).
This template currently selects SDK `10.0.300` with `rollForward` set to `latestFeature`.
See the Microsoft [`global.json` documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json) for SDK selection behavior.

Restore the local .NET tools from [`dotnet-tools.json`](dotnet-tools.json).

```powershell
dotnet tool restore
dotnet tool list
```

Run the important post-init tasks after creating a downstream project from this template.
The Aspire agent init task prepares local agent guidance for Aspire workflows and should be rerun when Aspire agent setup changes.

```powershell
dotnet husky run --group init
dotnet husky install
```

Use this as the usual downstream initialization sequence.

```powershell
dotnet tool restore
dotnet husky run --group init
dotnet husky install
dotnet restore Template.slnx --locked-mode
```

Restore, format-check, build, and test the solution manually.

```powershell
dotnet restore Template.slnx --locked-mode
dotnet format Template.slnx --verify-no-changes --verbosity diagnostic --no-restore
dotnet build Template.slnx --no-restore
dotnet test Template.slnx --no-build
```

Generate local Cobertura coverage files with the Coverlet collector.

```powershell
dotnet test Template.slnx --no-build --collect:"XPlat Code Coverage" --results-directory artifacts/coverage/local
```

## Tooling

### .NET Aspire CLI

[`Aspire.Cli`](https://learn.microsoft.com/en-us/dotnet/aspire/cli/overview) is available as the local `aspire` tool.
Use it for manual .NET Aspire workflows in downstream projects that add Aspire support.

```powershell
dotnet aspire --help
```

### Husky.NET

[Husky.NET](https://alirezanet.github.io/Husky.Net/) is available as the local `husky` tool.
The configured tasks live in [`.husky/task-runner.json`](.husky/task-runner.json).
See the Husky.NET [task runner documentation](https://alirezanet.github.io/Husky.Net/guide/task-runner.html) for task syntax.

Run the configured shell-script preparation task manually.
It updates staged `*.sh` files to be executable in the Git index and rejects staged shell scripts with CRLF or mixed line endings.

```powershell
dotnet husky run --name prepare-shell-scripts
```

Run the repository-wide shell-script permission task manually when existing tracked shell scripts need their Git executable bit repaired.
It updates every tracked `*.sh` file in the Git index, regardless of staged state.

```powershell
dotnet husky run --name update-shell-script-permissions
```

Run the configured init task group after creating a downstream project from this template.
The init group currently runs the .NET Aspire agent init task.
It runs `dotnet aspire agent init --skill-locations standard,claudecode --skills all --non-interactive` through the restored local Aspire CLI.

```powershell
dotnet husky run --group init
```

Run the .NET Aspire agent init task directly only when you need that single task.

```powershell
dotnet husky run --name dotnet-aspire-agent-init
```

Run the configured .NET format task manually.
It formats the solution with `dotnet format Template.slnx --verbosity diagnostic --no-restore`.

```powershell
dotnet husky run --name dotnet-format
```

Run the configured .NET format verification task manually.
It checks the same solution format rules without changing files.

```powershell
dotnet husky run --name dotnet-format-check
```

Install Husky hooks in a clone when you want Git to run configured hook commands automatically.

```powershell
dotnet husky install
```

Add the configured pre-commit task group to a pre-commit hook when a downstream project wants it to run before each commit.
The pre-commit group prepares staged shell scripts and verifies .NET formatting without changing files.

```powershell
dotnet husky add pre-commit -c "dotnet husky run --group pre-commit"
git add .husky/pre-commit .husky/task-runner.json
```

### `nektos/act`

[`act`](https://github.com/nektos/act) runs GitHub Actions locally through Docker.
Use it to validate workflow changes before pushing them to GitHub.

Install Docker and `act` first.
On Windows, install `act` with Winget or Scoop.

```powershell
winget install nektos.act
```

```powershell
scoop install act
```

Run the pull request test job locally.

```powershell
dotnet run --file scripts/act/test-pr.cs
```

Set `ACT_BIN` when `act` is installed outside your shell `PATH`.

```powershell
$env:ACT_BIN = "C:\path\to\act.exe"
dotnet run --file scripts/act/test-pr.cs
```

Run the `ci` branch push test job locally.

```powershell
dotnet run --file scripts/act/test-ci.cs
```

The quality jobs still provide useful local validation through `act`.
Current `act` versions can fail at `actions/upload-artifact@v7` with a `mime_type` schema error, so GitHub-hosted Actions remain the final verification path for artifact upload and step-summary rendering.

List the jobs that `act` can see.

```powershell
act -l pull_request -W .github/workflows/main.yaml
```

The repository `.actrc` contains only low-level runner defaults.
It maps `arkanis-runners` to an `act`-compatible Ubuntu runner image, not to the plain `ubuntu:latest` Docker image.
The plain Docker image does not include the Node runtime required by JavaScript actions.
The file-based C# scripts in [`scripts/act`](scripts/act) keep the human-facing commands named and readable.
They store local workflow artifacts under `.act/artifacts`.
Do not store real secrets in committed files.
Use `.act/secrets` or secure interactive secret prompts for local-only secrets.
Release and Kubernetes deployment workflows are not full local targets because they depend on GitHub release state, GHCR credentials, Kubernetes credentials, GitHub environments, and runner behavior that `act` does not completely emulate.
See [GitHub Actions](docs/github-actions.md) for the workflow map, runner trust rules, token permissions, release behavior, deployment behavior, and repository settings checklist.
Workflow changes are checked by `pipeline-quality.yaml` with actionlint.
Run `go install github.com/rhysd/actionlint/cmd/actionlint@v1.7.12`, add `$(go env GOPATH)/bin` to `PATH`, and then run `actionlint` for the same validation locally.

### `dotnet-setversion`

[`dotnet-setversion`](https://www.nuget.org/packages/dotnet-setversion/) is available as the local `setversion` tool.
Use it when a downstream project needs to update project or package versions manually.

```powershell
dotnet setversion --help
```
