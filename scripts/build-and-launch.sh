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

./scripts/build.sh "$BUILD_TYPE"
./scripts/install-mod.sh "$BUILD_TYPE"

cd ~/.local/share/Steam/steamapps/common/Valheim
./start_game_bepinex.sh \
    ~/.local/share/Steam/steamapps/common/Valheim/valheim.x86_64 \
    -force-glcore -console
