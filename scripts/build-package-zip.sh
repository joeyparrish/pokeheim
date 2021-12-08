#!/bin/bash

set -e
set -x

rm -rf staging Pokeheim.zip
mkdir -p staging/Pokeheim/Assets
cp Pokeheim/bin/Release/Pokeheim.dll staging/Pokeheim/
cp Pokeheim/Assets/*.png staging/Pokeheim/Assets/
cp -a Pokeheim/Assets/Translations staging/Pokeheim/Assets/
(cd staging; zip -r9 ../Pokeheim.zip Pokeheim/)
rm -rf staging
