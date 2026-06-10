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

Restore, build, and test the solution manually.

```powershell
dotnet restore Template.slnx
dotnet build Template.slnx --no-restore
dotnet test Template.slnx --no-build
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

Install Husky hooks in a clone when you want Git to run configured hook commands automatically.

```powershell
dotnet husky install
```

Add the existing task to a pre-commit hook when a downstream project wants it to run before each commit.

```powershell
dotnet husky add pre-commit -c "dotnet husky run --name prepare-shell-scripts"
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
.\scripts\act\test-pr.ps1
```

```bash
./scripts/act/test-pr.sh
```

Set `ACT_BIN` when `act` is installed outside your shell `PATH`.

```bash
ACT_BIN=/path/to/act ./scripts/act/test-pr.sh
```

Run the `ci` branch push test job locally.

```powershell
.\scripts\act\test-ci.ps1
```

```bash
./scripts/act/test-ci.sh
```

List the jobs that `act` can see.

```powershell
act -l pull_request -W .github/workflows/main.yaml
```

The repository `.actrc` contains only low-level runner defaults.
It maps `arkanis-runners` to an `act`-compatible Ubuntu runner image, not to the plain `ubuntu:latest` Docker image.
The plain Docker image does not include the Node runtime required by JavaScript actions.
The wrapper scripts in [`scripts/act`](scripts/act) keep the human-facing commands named and readable.
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
