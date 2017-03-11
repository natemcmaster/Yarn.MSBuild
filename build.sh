#!/usr/bin/env bash

set -e -o pipefail

config='Release'

netfxversion='4.6.0'
export ReferenceAssemblyRoot="$(pwd)/obj/refs/content/"
mkdir -p $ReferenceAssemblyRoot
if [ ! -e 'obj/refs.zip' ]; then
    wget -O obj/refs.zip https://dotnet.myget.org/F/aspnetcore-tools/api/v2/package/NETFrameworkReferenceAssemblies/$netfxversion
    unzip -q -d obj/refs/ obj/refs.zip
fi

source ./scripts/install-tools.sh
dotnet_home="$(pwd)/.dotnet"
export PATH="$dotnet_home:$PATH"
install_dotnet $dotnet_home 1.0.1

artifacts="$(pwd)/artifacts"
rm -r "$artifacts" 2>/dev/null && :
dotnet restore
dotnet msbuild /nologo src/Yarn.MSBuild/ /t:GetYarn
dotnet pack -c $config -o "$artifacts"
dotnet test -c $config test/Yarn.MSBuild.Tests/Yarn.MSBuild.Tests.csproj
