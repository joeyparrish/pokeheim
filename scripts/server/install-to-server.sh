#!/bin/bash

set -e

if [ "$1" == "" ]; then
  BUILD_TYPE=Debug
else
  BUILD_TYPE="$1"
fi

INSTANCE_NAME="instance-1"

cd "$(dirname "$0")"/../..

gcloud config configurations activate pokeheim

via_ssh() {
  gcloud compute ssh "$INSTANCE_NAME" -- -q "$@"
}

via_scp() {
  local ARGS_AND_INPUTS=( "${@:1:$#-1}" )
  local DESTINATION="${@:$#}"

  gcloud compute scp -- "${ARGS_AND_INPUTS[@]}" "$INSTANCE_NAME":"$DESTINATION"
}

via_ssh sudo systemctl stop pokeheim

PLUGINS_PATH=pokeheim-server/BepInEx/plugins

# New location (>= v1.0.4), simulates r2modman install pattern
POKEHEIM_FOLDER="$PLUGINS_PATH"/Pokeheim-Pokeheim
POKEHEIM_ASSETS="$PLUGINS_PATH"/Pokeheim-Pokeheim/Pokeheim/Assets

via_ssh rm -rf "$POKEHEIM_FOLDER"
via_ssh mkdir -p "$POKEHEIM_ASSETS"

# NOTE: What nuget downloads is always a Debug build of Jotunn.
# To install a true Release build, we need to fetch that from some place like
# Thunderstore.
if [ "$BUILD_TYPE" == "Release" ]; then
  via_scp $(./scripts/fetch-jotunn-release.sh) "$PLUGINS_PATH"/Jotunn.dll
else
  via_scp Pokeheim/bin/$BUILD_TYPE/Jotunn.dll "$PLUGINS_PATH"/
fi

via_scp Pokeheim/bin/$BUILD_TYPE/Pokeheim.dll "$POKEHEIM_FOLDER"/
via_scp Pokeheim/Assets/*.png "$POKEHEIM_ASSETS"/
via_scp Pokeheim/Assets/*.mp3 "$POKEHEIM_ASSETS"/
via_scp -r Pokeheim/Assets/Translations "$POKEHEIM_ASSETS"/

via_ssh sudo systemctl start pokeheim

echo "Installed $BUILD_TYPE build on remote server and restarted service."
