#!/bin/bash

set -e

cd "$(dirname "$0")"/../..
./scripts/server/vm-ssh.sh -q curl ifconfig.me
echo
