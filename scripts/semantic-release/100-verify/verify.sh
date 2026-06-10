#!/usr/bin/env bash
set -eEuo pipefail

THIS_DIR="$(dirname "$(realpath "$0")")"

. "${THIS_DIR}/verify-common.sh"

# Entry point for semantic-release verifyReleaseCmd.
# This phase checks tools and configured release targets without creating publish artifacts.
### verifyReleaseCmd
#
#| Command property | Description                                                              |
#| ---------------- | ------------------------------------------------------------------------ |
#| `exit code`      | `0` if the verification is successful, or any other exit code otherwise. |
#| `stdout`         | Only the reason for the verification to fail can be written to `stdout`. |
#| `stderr`         | Can be used for logging.                                                 |

[[ -n "${DEBUG+x}" ]] && env 1>&2

require_env "VERSION"
require_env "VERSION_TAG"
require_env "VERSION_CHANNEL"

>&2 echo "Restoring .NET tools..."
dotnet tool restore 1>&2 # logging output must not go to stdout

run_subs_with_prefix "${THIS_DIR}" "1"
