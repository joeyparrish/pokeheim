#!/bin/bash

VALHEIM_DATA=~/.local/share/Steam/steamapps/common/Valheim/valheim_Data

> ref.cs
> ref.il

# SoftReferenceableAssets and gui_framework did not exist in 2022.  They were
# added along with the soft-referenced (lazily loaded) prefab system, which
# several of our patches have to reckon with, so we dump them too.
for i in \
    Managed/assembly_valheim.dll \
    Managed/assembly_guiutils.dll \
    Managed/SoftReferenceableAssets.dll \
    Managed/gui_framework.dll \
; do
  echo "Dumping $i C#"
  ilspycmd     $VALHEIM_DATA/$i >> ref.cs
  echo "Dumping $i IL"
  ilspycmd -il $VALHEIM_DATA/$i >> ref.il
done
