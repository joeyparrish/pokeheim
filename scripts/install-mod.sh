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

PLUGINS_PATH=~/.local/share/Steam/steamapps/common/Valheim/BepInEx/plugins

cd "$(dirname "$0")"/..

rm -rf $PLUGINS_PATH/Pokeheim/
mkdir -p $PLUGINS_PATH/Pokeheim/Assets/

# NOTE: This is always a Debug build of Jotunn, because that's what nuget
# downloads.  A Release build of Jotunn will be used when Pokeheim is installed
# through Thunderstore or similar.
cp Pokeheim/bin/$BUILD_TYPE/Jotunn.dll $PLUGINS_PATH/
cp Pokeheim/bin/$BUILD_TYPE/Pokeheim.dll $PLUGINS_PATH/Pokeheim/
cp Pokeheim/Assets/*.png $PLUGINS_PATH/Pokeheim/Assets/
cp -a Pokeheim/Assets/Translations $PLUGINS_PATH/Pokeheim/Assets/
