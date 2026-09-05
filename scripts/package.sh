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
rm -rf Pokeheim/bin/ Pokeheim/obj/
./scripts/build.sh Release

# SDK-style projects put output under a target-framework subdirectory.
BUILD_OUTPUT=Pokeheim/bin/Release/net462

# Make a generic zip.
rm -rf staging Pokeheim.zip
mkdir -p staging/plugins/Pokeheim/Assets
# Stage mod & assets.
cp "$BUILD_OUTPUT"/Pokeheim.dll staging/plugins/
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
#
# We used to read the version back out of the built DLL with monodis, but that
# is a mono tool and we no longer install mono.  Instead we check the source
# file the build generated from $RELEASE_VERSION, which is what the assembly
# version is compiled from.  That still catches the case this guards against:
# a stale build whose version does not match the manifest we are shipping.
manifest_version=$(cat staging/manifest.json | jq -r .version_number)
generated=Pokeheim/obj/Release/net462/ReleaseVersion.g.cs

if [ ! -f "$generated" ]; then
  echo "Expected the build to generate $generated" 1>&2
  exit 1
fi

built_version=$(grep -oE '"[0-9]+\.[0-9]+\.[0-9]+"' "$generated" | tr -d '"')
if [[ "$manifest_version" != "$built_version" ]]; then
  echo "Version mismatch!"
  echo "  Manifest version $manifest_version"
  echo "  Built version $built_version"
  exit 1
fi
