#!/bin/bash

set -e

gcloud config configurations activate pokeheim

gcloud compute ssh instance-1 -- -q curl ifconfig.me
echo
