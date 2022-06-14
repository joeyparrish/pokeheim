#!/bin/bash

# Written for me (Joey), and working on Ubuntu 20.04 LTS.  I make no promises
# that it will work for you.  See .github/workflows/ for repeatable
# instructions to install necessary .NET tools.

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

msbuild Pokeheim.sln /p:Configuration="$BUILD_TYPE"
