#!/bin/sh
# BepInEx-specific settings
# NOTE: Do not edit unless you know what you are doing!
####
export DOORSTOP_ENABLE=TRUE
export DOORSTOP_INVOKE_DLL_PATH=./BepInEx/core/BepInEx.Preloader.dll
export DOORSTOP_CORLIB_OVERRIDE_PATH=./unstripped_corlib

export LD_LIBRARY_PATH="./doorstop_libs:$LD_LIBRARY_PATH"
export LD_PRELOAD="libdoorstop_x64.so:$LD_PRELOAD"
####

export LD_LIBRARY_PATH="./linux64:$LD_LIBRARY_PATH"
export SteamAppId=892970

echo "Starting server PRESS CTRL-C to exit"

# Customize these.
PASSWORD="catchemall"  # Minimum 5 characters, can't be in $NAME
NAME="Pokeheim"
WORLD="Pokeserver"
PORT="2456"  # Open through firewall on TCP & UDP

exec ./valheim_server.x86_64 -name "$NAME" -port "$PORT" -world "$WORLD" -password "$PASSWORD"
