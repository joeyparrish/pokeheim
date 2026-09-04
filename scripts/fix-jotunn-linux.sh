#!/bin/bash

# WORKAROUND, REMOVE ME WHEN FIXED UPSTREAM.
#
# Jotunn's JotunnBuildTask, which publicizes the game assemblies at build time,
# hardcodes the game data directory as "Valheim_Data" with a capital V:
#
#   JotunnBuildTask/JotunnBuildTask.cs:
#     internal const string ValheimData = "Valheim_Data";
#
# On Windows and macOS the filesystem is case-insensitive, so that matches the
# real directory.  On Linux it does not, the Directory.Exists check fails, and
# the build dies with a singularly unhelpful message:
#
#   error MSB4181: The "JotunnBuildTask" task returned false but did not log
#   an error.
#
# Note that Jotunn's own Paths.props gets this right, probing both spellings
# when it sets VALHEIM_MANAGED.  Only the build task is wrong.
#
# A fix has been sent upstream.  Until it ships in a Jotunn release, this
# script creates a capitalized symlink alongside the real directory so the
# task can find it.  When the fix lands, delete this script and its call in
# scripts/build.sh.
#
# CI does not need this: it builds against the dedicated server, whose
# directory is named valheim_server_Data, which the task already accepts.  Just
# don't rename it to valheim_Data.

set -e

VALHEIM_INSTALL="${VALHEIM_INSTALL:-$HOME/.local/share/Steam/steamapps/common/Valheim}"

if [ ! -d "$VALHEIM_INSTALL" ]; then
  echo "Valheim install not found at $VALHEIM_INSTALL" 1>&2
  exit 1
fi

# Nothing to do if the game already presents a directory the task accepts.
if [ -e "$VALHEIM_INSTALL/Valheim_Data" ]; then
  exit 0
fi

if [ -e "$VALHEIM_INSTALL/valheim_server_Data" ]; then
  exit 0
fi

if [ ! -d "$VALHEIM_INSTALL/valheim_Data" ]; then
  echo "No valheim_Data directory in $VALHEIM_INSTALL" 1>&2
  exit 1
fi

ln -sfn "$VALHEIM_INSTALL/valheim_Data" "$VALHEIM_INSTALL/Valheim_Data"
echo "Created Valheim_Data symlink to work around Jotunn issue on Linux."
