#!/bin/bash

cp "$1" "$1.orig"
pngcrush -rem allb -brute -reduce "$1.orig" "$1"
optipng -o7 "$1"
wc -c "$1.orig"
wc -c "$1"
