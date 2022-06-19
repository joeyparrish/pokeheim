#!/bin/bash

# Written for me (Joey), and working on Ubuntu 20.04 LTS.  I make no promises
# that it will work for you.  See .github/workflows/ for repeatable
# instructions to install necessary .NET tools.

set -e

cd ~/.local/share/Steam/steamapps/common/Valheim
~/.local/share/Steam/steamapps/common/Valheim/valheim.x86_64 \
    -force-glcore -console
