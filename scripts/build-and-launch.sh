#!/bin/bash

# Written for me (Joey), and working on Ubuntu 20.04 LTS.  I make no promises
# that it will work for you.  See .github/workflows/ for repeatable
# instructions to install necessary .NET tools.

set -e

BUILD_TYPE=Debug
PLUGINS_PATH=~/.local/share/Steam/steamapps/common/Valheim/BepInEx/plugins

cd "$(dirname "$0")"/..

msbuild Pokeheim.sln /p:Configuration=Release
msbuild Pokeheim.sln /p:Configuration=Debug

rm -rf $PLUGINS_PATH/Pokeheim/
mkdir -p $PLUGINS_PATH/Pokeheim/Assets/

cp Pokeheim/bin/$BUILD_TYPE/Jotunn.dll $PLUGINS_PATH/
cp Pokeheim/bin/$BUILD_TYPE/Pokeheim.dll $PLUGINS_PATH/Pokeheim/
cp Pokeheim/Assets/*.png $PLUGINS_PATH/Pokeheim/Assets/
cp -a Pokeheim/Assets/Translations $PLUGINS_PATH/Pokeheim/Assets/

cd ~/.local/share/Steam/steamapps/common/Valheim
./start_game_bepinex.sh ~/.local/share/Steam/steamapps/common/Valheim/valheim.x86_64 -force-glcore -console
