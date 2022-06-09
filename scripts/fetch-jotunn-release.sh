#!/bin/bash

set -e

cd "$(dirname "$0")"/..

function get_version() {
  monodis --assembly "$1" 2>/dev/null | grep Version | tr -d ' ' \
      | cut -f 2 -d : | cut -f 1-3 -d .
}

CACHE_PATH=~/.Jotunn-release.dll
DESIRED_JOTUNN_VERSION=$(get_version Pokeheim/bin/Release/Jotunn.dll)
CACHED_JOTUNN_VERSION=$(get_version $CACHE_PATH)
JOTUNN_URL_BASE=https://valheim.thunderstore.io/package/download/ValheimModding/Jotunn

if [ "$CACHED_JOTUNN_VERSION" != "$DESIRED_JOTUNN_VERSION" ]; then
  wget $JOTUNN_URL_BASE/$DESIRED_JOTUNN_VERSION/ -O $CACHE_PATH.zip
  unzip -p $CACHE_PATH.zip plugins/Jotunn.dll > $CACHE_PATH
fi

echo "$CACHE_PATH"
