#!/bin/bash

set -e

DEP="$1"

if [ -z "$DEP" ]; then
  echo "Specify a dependency." 1>&2
  exit 1
fi

cd "$(dirname "$0")"/..

# Extract a string from the dependencies section of the manifest, such as
# "denikson-BepInExPack_Valheim-5.4.1901", split it on hyphens, and output the
# version number part like "5.4.1901".
cat publish/manifest.json | \
  jq -r ".dependencies[]|select(.|contains(\"$DEP\"))" | \
  cut -d - -f 3
