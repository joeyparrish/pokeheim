#!/bin/bash

# Written for me (Joey), and working on Ubuntu 20.04 LTS.  I make no promises
# that it will work for you.

set -e

cd "$(dirname "$0")"/../

nuget restore
nuget update Pokeheim/packages.config
