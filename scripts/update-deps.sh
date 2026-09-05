#!/bin/bash

# Written for me (Joey), and working on Ubuntu 24.04 LTS.  I make no promises
# that it will work for you.
#
# Dependencies are PackageReferences in Pokeheim/Pokeheim.csproj now, not a
# packages.config, so there is nothing to "update" in place.  This restores
# them and then reports anything newer, leaving the decision to you: bumping a
# dependency is a change we want in a commit, not a side effect of running a
# script.
#
# Remember that publish/manifest.json pins the versions end users install, and
# has to be bumped to match by hand.

set -e

cd "$(dirname "$0")"/..

if command -v dotnet >/dev/null 2>&1; then
  DOTNET=dotnet
elif [ -x "$HOME/.dotnet/dotnet" ]; then
  DOTNET="$HOME/.dotnet/dotnet"
else
  echo "No dotnet SDK found.  Run ./scripts/install-linux-tools.sh" 1>&2
  exit 1
fi

"$DOTNET" restore Pokeheim.sln

echo
echo "Outdated packages, if any:"
"$DOTNET" list Pokeheim.sln package --outdated

echo
echo "Versions pinned for end users in publish/manifest.json:"
jq -r '.dependencies[]' publish/manifest.json | sed 's/^/    /'
