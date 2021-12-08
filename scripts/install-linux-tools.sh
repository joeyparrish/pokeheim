#!/bin/bash

set -e
set -x

sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
echo "deb https://download.mono-project.com/repo/ubuntu stable-bionic main" | sudo tee /etc/apt/sources.list.d/mono-official-stable.list
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg --force-confnew -i packages-microsoft-prod.deb
sudo apt-get -y update || true
sudo apt-get -y install mono-roslyn mono-complete mono-dbg msbuild nuget unzip dirmngr dotnet-sdk-5.0 dotnet-runtime-5.0
sudo nuget update -self
nuget sources Add -Name nuget.org -Source https://api.nuget.org/v3/index.json || true
