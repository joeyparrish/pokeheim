#!/bin/bash

set -e

cd "$(dirname "$0")"/..

# Extract a string from the dependencies section of the manifest, such as
# "denikson-BepInExPack_Valheim-5.4.1901", split it on hyphens, and output the
# version number part like "5.4.1901".
cat publish/manifest.json | \
  jq -r '.dependencies[]|select(.|contains("denikson-BepInExPack_Valheim"))' | \
  cut -d - -f 3
