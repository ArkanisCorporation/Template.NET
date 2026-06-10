#!/bin/sh
set -eu

# Prepares staged shell scripts before commit.
# The hook marks staged *.sh files executable and blocks CRLF or mixed line endings.

# Only staged shell scripts need preparation.
tracked_shell_scripts="$(git diff --cached --name-only --diff-filter=ACMR -- '*.sh')"

if [ -z "$tracked_shell_scripts" ]; then
    exit 0
fi

# File mode must be changed in the Git index so the commit records +x.
printf '%s\n' "$tracked_shell_scripts" | while IFS= read -r file; do
    git update-index --chmod=+x -- "$file"
done

# The loop runs in a subshell, so collect failures in a temporary file.
failed_file="$(mktemp)"
trap 'rm -f "$failed_file"' EXIT

# Git's staged EOL metadata catches CRLF before the file reaches CI.
printf '%s\n' "$tracked_shell_scripts" | while IFS= read -r file; do
    if git ls-files --eol -- "$file" | grep -Eq '^i/(crlf|mixed)[[:space:]]'; then
        printf '%s\n' "$file" >> "$failed_file"
    fi
done

if [ -s "$failed_file" ]; then
    echo "Shell scripts must use LF line endings before commit:"
    sed 's/^/  - /' "$failed_file"
    echo "Convert the files to LF and stage them again."
    exit 1
fi
