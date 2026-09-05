#!/bin/bash

# Written for me (Joey), and working on Ubuntu 24.04 LTS.  I make no promises
# that it will work for you.
#
# Installs the .NET SDK into your home directory, plus the decompiler used by
# scripts/dump-valheim.sh.  Nothing here needs root, and nothing is installed
# system-wide: everything lands in ~/.dotnet, which you can delete to undo it.
#
# We no longer need mono, msbuild, or nuget.  The project is an SDK-style
# project built with "dotnet build", and its dependencies come from NuGet
# PackageReferences rather than a packages.config.

set -e

DOTNET_ROOT="$HOME/.dotnet"

if [ -x "$DOTNET_ROOT/dotnet" ]; then
  echo "dotnet SDK already installed at $DOTNET_ROOT"
else
  echo "Installing the .NET SDK to $DOTNET_ROOT ..."
  INSTALLER=$(mktemp)
  trap 'rm -f "$INSTALLER"' EXIT
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o "$INSTALLER"
  chmod +x "$INSTALLER"
  "$INSTALLER" --channel LTS --install-dir "$DOTNET_ROOT"
fi

export DOTNET_ROOT
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"

# ilspycmd decompiles the game assemblies for scripts/dump-valheim.sh.
if command -v ilspycmd >/dev/null 2>&1; then
  echo "ilspycmd already installed"
else
  echo "Installing ilspycmd ..."
  dotnet tool install --global ilspycmd
fi

echo
echo "Done.  Add this to your shell profile if it is not there already:"
echo
echo "    export DOTNET_ROOT=\"\$HOME/.dotnet\""
echo "    export PATH=\"\$DOTNET_ROOT:\$DOTNET_ROOT/tools:\$PATH\""
echo
echo "scripts/build.sh will find the SDK either way."
