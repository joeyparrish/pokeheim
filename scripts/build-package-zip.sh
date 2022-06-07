#!/bin/bash

set -e
set -x

cd "$(dirname "$0")"/..

./scripts/build.sh Release

rm -rf staging Pokeheim.zip
mkdir -p staging/Pokeheim/Assets
cp Pokeheim/bin/Release/Pokeheim.dll staging/Pokeheim/
cp Pokeheim/Assets/*.png staging/Pokeheim/Assets/
cp Pokeheim/Assets/*.mp3 staging/Pokeheim/Assets/
cp Pokeheim/Package/{icon.png,manifest.json} staging/
cp README.md staging/
cp -a Pokeheim/Assets/Translations staging/Pokeheim/Assets/
(cd staging; zip -r9 ../Pokeheim.zip *)

set +x
manifest_version=$(cat staging/manifest.json | jq -r .version_number)
dll_version=$(monodis --assembly staging/Pokeheim/Pokeheim.dll \
              | grep Version | cut -f 2 -d : | tr -d ' ')

if [[ "$manifest_version.0" != "$dll_version" ]]; then
  echo "Version mismatch!"
  echo "  Manifest version $manifest_version"
  echo "  DLL version $dll_version"
  exit 1
fi

rm -rf staging
