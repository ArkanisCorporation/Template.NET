#!/usr/bin/env bash
set -eEuo pipefail

THIS_DIR="$(dirname "$(realpath "$0")")"

. "${THIS_DIR}/../common.sh"

# Entry point for semantic-release prepareCmd.
# This phase applies the release version and creates artifacts consumed by publishCmd.
### prepareCmd
#
#| Command property | Description                                                                                                         |
#| ---------------- | ------------------------------------------------------------------------------------------------------------------- |
#| `exit code`      | Any non `0` code is considered as an unexpected error and will stop the `semantic-release` execution with an error. |
#| `stdout`         | Can be used for logging.                                                                                            |
#| `stderr`         | Can be used for logging.                                                                                            |

require_env "VERSION"
require_env "VERSION_TAG"
require_env "VERSION_CHANNEL"

>&2 echo "Applying the current release version ${VERSION} recursively..."
dotnet setversion --recursive "${VERSION}" 1>&2 # logging output must not go to stdout

run_subs_with_prefix "${THIS_DIR}" "2"
