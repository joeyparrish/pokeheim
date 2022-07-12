#!/bin/bash

set -e

cd "$(dirname "$0")"/..
./scripts/vm-ssh.sh -q curl ifconfig.me
echo
