#!/bin/bash

# What NuGet gives us is always a Debug build of Jotunn.  To ship a true
# Release build we fetch it from Thunderstore instead.  Prints the path to a
# cached copy.
#
# The version comes from the PackageReference in the csproj, which is the
# authoritative statement of what we build against.  We used to read it out of
# the built DLL with monodis, but that is a mono tool and we no longer install
# mono.

set -e

cd "$(dirname "$0")"/..

CSPROJ=Pokeheim/Pokeheim.csproj

VERSION=$(grep -oE 'Include="JotunnLib" Version="[0-9.]+"' "$CSPROJ" \
          | grep -oE '[0-9]+\.[0-9]+\.[0-9]+')

if [ -z "$VERSION" ]; then
  echo "Could not find the JotunnLib version in $CSPROJ" 1>&2
  exit 1
fi

# The version is part of the cache path, so a version bump simply misses the
# cache instead of needing a separate freshness check.
CACHE_PATH=~/.Jotunn-release-$VERSION.dll
URL_BASE=https://thunderstore.io/package/download/ValheimModding/Jotunn

if [ ! -f "$CACHE_PATH" ]; then
  TEMP_ZIP=$(mktemp)
  trap 'rm -f "$TEMP_ZIP"' EXIT
  curl -fsSL "$URL_BASE/$VERSION/" -o "$TEMP_ZIP"
  unzip -p "$TEMP_ZIP" plugins/Jotunn.dll > "$CACHE_PATH"
fi

echo "$CACHE_PATH"
