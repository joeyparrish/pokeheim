#!/bin/bash

set -e

cd "$(dirname "$0")"/..

VERSION=$(./scripts/get-dep-version.sh Oran1-Mountup)
PLUGINS_PATH="$HOME/.local/share/Steam/steamapps/common/Valheim/BepInEx/plugins/"

wget -O mountup.zip "https://valheim.thunderstore.io/package/download/Oran1/Mountup/$VERSION/"

unzip mountup.zip MountUp.dll -d "$PLUGINS_PATH"
