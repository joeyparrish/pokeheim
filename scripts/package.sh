#!/bin/bash

if [ -z "$RELEASE_VERSION" ]; then
  echo "You must set the environment variable \$RELEASE_VERSION," 1>&2
  echo "and use semantic versioning." 1>&2
  exit 1
fi

# Fail on error.
set -e

# Go to the project's root directory.
cd "$(dirname "$0")"/..

# Log steps.
set -x

# Build a clean release.
rm -rf Pokeheim/bin/
./scripts/build.sh Release

# Make a generic zip.
rm -rf staging Pokeheim.zip
mkdir -p staging/plugins/Pokeheim/Assets
# Stage mod & assets.
cp Pokeheim/bin/Release/Pokeheim.dll staging/plugins/
cp Pokeheim/Assets/*.png staging/plugins/Pokeheim/Assets/
cp Pokeheim/Assets/*.mp3 staging/plugins/Pokeheim/Assets/
cp -a Pokeheim/Assets/Translations staging/plugins/Pokeheim/Assets/
# Stage mod metadata.
cp publish/icon.png staging/
cp README.md staging/
cat publish/manifest.json \
    | jq ".version_number = \"$RELEASE_VERSION\"" \
    > staging/manifest.json
# Zip it.
(cd staging; zip -r9 ../Pokeheim.zip *)

# Stop logging.
set +x

# Double-check versioning.
manifest_version=$(cat staging/manifest.json | jq -r .version_number)
dll_version=$(monodis --assembly staging/plugins/Pokeheim.dll \
              | grep Version | cut -f 2 -d : | tr -d ' ')
if [[ "$manifest_version.0" != "$dll_version" ]]; then
  echo "Version mismatch!"
  echo "  Manifest version $manifest_version"
  echo "  DLL version $dll_version"
  exit 1
fi
