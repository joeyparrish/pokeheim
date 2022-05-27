#!/bin/bash

VALHEIM_DATA=~/.local/share/Steam/steamapps/common/Valheim/valheim_Data

> ref.cs
> ref.il

for i in \
    Managed/assembly_valheim.dll \
    Managed/assembly_guiutils.dll \
; do
  echo "Dumping $i C#"
  ilspycmd     $VALHEIM_DATA/$i >> ref.cs
  echo "Dumping $i IL"
  ilspycmd -il $VALHEIM_DATA/$i >> ref.il
done
