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

LOCAL_PATH="$HOME/.local/share/Steam/steamapps/common/Valheim dedicated server"
SERVER_PATH=pokeheim-server

via_ssh sudo systemctl stop pokeheim

via_scp -r "$LOCAL_PATH"/* "$SERVER_PATH"/

via_ssh wget -O bepinex.zip "https://valheim.thunderstore.io/package/download/denikson/BepInExPack_Valheim/$(scripts/get-bepinex-version.sh)/"

via_ssh unzip -d bepinex bepinex.zip

via_ssh cp -a bepinex/BepInExPack_Valheim/* pokeheim-server/

via_ssh rm -rf bepinex

via_ssh sudo systemctl start pokeheim

echo "Updated remote server and restarted service."
