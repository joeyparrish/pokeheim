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

# Valheim initializes the Steam API on startup and exits immediately if the
# Steam client is not running.  It does this silently enough that it looks
# like a mod problem, so check up front and say so plainly.
if ! pgrep -x steam >/dev/null 2>&1; then
  echo "Steam does not appear to be running." 1>&2
  echo "Valheim will exit at startup without it.  Start Steam first." 1>&2
  exit 1
fi

cd ~/.local/share/Steam/steamapps/common/Valheim
# NOTE: we used to pass -force-glcore here, which forces the legacy OpenGL
# Core renderer.  As of 2026 that segfaults on startup inside
# libnvidia-glcore.so, with no managed frames involved.  Let Unity choose its
# own renderer.
./start_game_bepinex.sh \
    ~/.local/share/Steam/steamapps/common/Valheim/valheim.x86_64 \
    -console
