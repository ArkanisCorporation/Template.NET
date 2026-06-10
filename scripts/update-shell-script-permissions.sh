#!/bin/sh
set -eu

# Marks every tracked shell script executable in the Git index.
# This is a manual repository-wide companion to the staged pre-commit preparation.

repo_root="$(git rev-parse --show-toplevel)"
cd "$repo_root"

tracked_shell_scripts="$(git ls-files -- '*.sh')"

if [ -z "$tracked_shell_scripts" ]; then
    exit 0
fi

printf '%s\n' "$tracked_shell_scripts" | while IFS= read -r file; do
    git update-index --chmod=+x -- "$file"
done

echo "Marked tracked shell scripts executable in the Git index:"
printf '%s\n' "$tracked_shell_scripts" | sed 's/^/  - /'
