#!/bin/bash

set -e

gcloud config configurations activate pokeheim

gcloud compute instances start instance-1
