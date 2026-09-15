#!/bin/bash

# Written for me (Joey), and working on Ubuntu 24.04 LTS.  I make no promises
# that it will work for you.  See .github/workflows/ for repeatable
# instructions to install necessary .NET tools, or run
# ./scripts/install-linux-tools.sh.

set -e

if [ "$1" == "" ]; then
  BUILD_TYPE=Debug
else
  BUILD_TYPE="$1"
fi

cd "$(dirname "$0")"/..

if [ "$BUILD_TYPE" == "Release" ]; then
  if [ -z "$RELEASE_VERSION" ]; then
    echo "You must set the environment variable \$RELEASE_VERSION," 1>&2
    echo "and use semantic versioning." 1>&2
    exit 1
  fi
fi

# The dotnet SDK installs to ~/.dotnet, which puts ~/.dotnet/tools on your PATH
# but not the SDK itself.  Find it either way.
if command -v dotnet >/dev/null 2>&1; then
  DOTNET=dotnet
elif [ -x "$HOME/.dotnet/dotnet" ]; then
  DOTNET="$HOME/.dotnet/dotnet"
else
  echo "No dotnet SDK found.  Run ./scripts/install-linux-tools.sh" 1>&2
  exit 1
fi

# Warnings we have looked at and decided to live with.  Each is a regular
# expression matched against the warning's own message.  Anything not listed
# here fails the build.
#
# Keeping this list, rather than suppressing warnings in the csproj, is
# deliberate.  MSBuild cannot suppress a warning for one assembly, so silencing
# MSB3277 would also silence a real clash between, say, two versions of
# Mono.Cecil or Harmony arriving through Jotunn.  That class of conflict
# compiles cleanly and then throws MissingMethodException at runtime, which is
# painful to trace back.  Tolerating one known warning by name keeps the rest
# of the signal.
ALLOWED_WARNINGS=(
  # Indented lines, the full tree for any given version warning.  Not a
  # headline warning, but needed here to suppress the output in normal
  # operation.
  'MSB3277:  '

  # We target net462 because that is the only framework Jotunn ships, and
  # net462 supplies the netstandard 2.0 facade while the game's UnityEngine.dll
  # was built against 2.1.  MSBuild picks 2.0, which is correct for us: we use
  # no APIs that exist only in 2.1.
  'MSB3277: .*"netstandard'
)

ALLOWED_PATTERN=$(printf '%s|' "${ALLOWED_WARNINGS[@]}")
ALLOWED_PATTERN="${ALLOWED_PATTERN%|}"

LOG=$(mktemp)
PERMANENT_LOG="/tmp/pokeheim-build.log"
trap 'rm -f "$LOG"' EXIT

set -o pipefail
"$DOTNET" build Pokeheim.sln /p:Configuration="$BUILD_TYPE" 2>&1 | \
    tee "$LOG" | \
    grep -vE "$ALLOWED_PATTERN"
set +o pipefail

# Only look at the warnings themselves.  MSB3277 prints its whole conflict
# graph as indented continuation lines beneath a single real warning, so ignore
# any line whose message starts with whitespace, and drop the trailing
# "[/path/to/project.csproj]" that MSBuild appends.
WARNINGS=$(grep -oE 'warning [A-Z]+[0-9]+: [^ ].*' "$LOG" \
           | sed 's/ \[\/.*//' \
           | sort -u || true)

if [ -n "$WARNINGS" ]; then
  UNEXPECTED=$(echo "$WARNINGS" | grep -vE "$ALLOWED_PATTERN" || true)

  if [ -n "$UNEXPECTED" ]; then
    echo 1>&2
    echo "Unexpected build warnings:" 1>&2
    echo "$UNEXPECTED" | sed 's/^/    /' 1>&2
    echo 1>&2
    echo "Fix them, or add them to ALLOWED_WARNINGS in $0 with a note" 1>&2
    echo "saying why they are safe." 1>&2
    echo 1>&2
    echo "See $PERMANENT_LOG for full details." 1>&2
    cp "$LOG" "$PERMANENT_LOG"
    exit 1
  fi
fi
