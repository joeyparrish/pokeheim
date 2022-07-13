#!/bin/bash

set -e
set -x

INSTANCE_NAME="instance-1"
WORLD="$1"
WORLD_PATH=".config/unity3d/IronGate/Valheim/worlds_local"

if [ -z "$WORLD" ]; then
  echo "Specify the name of a world." 1>&2
  exit 1
fi

if [ ! -e ~/"$WORLD_PATH"/"$WORLD".db ]; then
  echo "World \"$WORLD\" not found." 1>&2
  exit 1
fi

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

via_ssh mkdir -p "$WORLD_PATH"
via_scp ~/"$WORLD_PATH"/"$WORLD".* "$WORLD_PATH"/

via_ssh sudo systemctl start pokeheim

echo "Transferred \"$WORLD\" to remote server and restarted service."
