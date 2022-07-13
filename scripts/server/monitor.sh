#!/bin/bash

set -e

cd "$(dirname "$0")"/../..
./scripts/server/vm-ssh.sh journalctl -u pokeheim -f --output cat

