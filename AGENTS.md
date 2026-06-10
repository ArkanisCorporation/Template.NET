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

## Shell Script Permissions

Run `dotnet husky run --name update-shell-script-permissions` when tracked `*.sh` files need their Git executable bit repaired across the repository.
Keep the pre-commit shell preparation task scoped to staged `*.sh` files.
