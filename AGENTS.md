# Agent Context

## Instruction Stewardship

Keep shared repo context current without turning `AGENTS.md` into a component catalogue.
While implementing, testing, debugging, or reviewing, ask:

> Did I learn a durable convention, setup step, repeated pitfall, design expectation, or local pattern that applies across multiple components, interfaces, workflows, or subsystems?

If yes, update the nearest applicable `AGENTS.md`; also update `README.md` when the context helps human developers or operators.
Keep updates concise and factual; exclude transient output, speculation, secrets, one-off task history, and component-only advice.
If no documentation update is needed, say so in the final handoff.

For durable contracts on public types, members, or model invariants, add XML docs there: `<summary>` for meaning, `<remarks>` for caveats, constraints, lifecycle, and side effects, and `<exception>` for thrown exceptions.
Keep `AGENTS.md` focused on workflow, repo conventions, cross-file patterns, design expectations, and pointers to code owning broader contracts.

## GitHub Actions

Keep reusable workflow files directly under `.github/workflows`.
GitHub does not support reusable workflow files in subdirectories.

Keep GitHub Actions changes reflected in [`README.md`](README.md) or a dedicated workflow operations document when local validation commands, runner expectations, secrets, or deployment behavior change.

## Line Endings

Keep repository text files on LF line endings.
`.editorconfig` defines the editor expectation, and `.gitattributes` keeps fresh Git checkouts consistent even when a developer has `core.autocrlf=true`.

## Repository Automation Scripts

Keep repository automation scripts as C# file-based apps run with `dotnet run --file`.
Executable file-based script entries start with `#!/usr/bin/env -S dotnet --`, followed by the shared `#:` property directives used under `scripts`.
Use `#:include` for shared script logic.
Use `CliWrap` through the shared native-command helpers for native command execution.
Use `LibGit2Sharp` through the shared repository helpers for Git repository, status, blob, and index operations.
When a file-based script name contains characters that are awkward for generated assembly names, set an explicit `#:property AssemblyName=...`.
Public and internal script helper APIs and custom exceptions require XML docs that state behavior, preconditions, side effects, and thrown exceptions.

## Post-Init Tasks

Run `dotnet husky run --group init` and `dotnet husky install` after creating a downstream project or worktree from this template.
Keep essential post-init tasks in the Husky `init` group, including `dotnet-aspire-agent-init`, so README initialization examples can point at one stable command.
