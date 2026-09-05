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

# WORKAROUND, REMOVE ME WHEN FIXED UPSTREAM.  See the script for details.
./scripts/fix-jotunn-linux.sh

"$DOTNET" build Pokeheim.sln /p:Configuration="$BUILD_TYPE"
