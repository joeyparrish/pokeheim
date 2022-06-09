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

if [ "$2" == "" ]; then
  GAME_NAME="Valheim"
else
  GAME_NAME="$2"
fi

PLUGINS_PATH=~/.local/share/Steam/steamapps/common/"$GAME_NAME"/BepInEx/plugins

cd "$(dirname "$0")"/..

rm -rf "$PLUGINS_PATH"/Pokeheim/
mkdir -p "$PLUGINS_PATH"/Pokeheim/Assets/

# NOTE: What nuget downloads is always a Debug build of Jotunn.
# To install a true Release build, we need to fetch that from some place like
# Thunderstore.
if [ "$BUILD_TYPE" == "Release" ]; then
  cp $(./scripts/fetch-jotunn-release.sh) "$PLUGINS_PATH"/Jotunn.dll
else
  cp Pokeheim/bin/$BUILD_TYPE/Jotunn.dll "$PLUGINS_PATH"/
fi
cp Pokeheim/bin/$BUILD_TYPE/Pokeheim.dll "$PLUGINS_PATH"/Pokeheim/
cp Pokeheim/Assets/*.png "$PLUGINS_PATH"/Pokeheim/Assets/
cp Pokeheim/Assets/*.mp3 "$PLUGINS_PATH"/Pokeheim/Assets/
cp -a Pokeheim/Assets/Translations "$PLUGINS_PATH"/Pokeheim/Assets/

echo "Installed $BUILD_TYPE build."
