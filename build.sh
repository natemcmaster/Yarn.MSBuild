#!/usr/bin/env bash

set -e -o pipefail
source ./scripts/common.sh

if [[ "$TRAVIS_BUILD_NUMBER" != "" && "$TRAVIS_BRANCH" != "master" ]]; then
    build_number=$(printf "%04d" $TRAVIS_BUILD_NUMBER)
    echo "BuildNumber=$build_number"
    export BuildNumber=$build_number
fi
config='Release'

netfxversion='4.6.0'
export ReferenceAssemblyRoot="$(pwd)/obj/refs/content/"
mkdir -p $ReferenceAssemblyRoot
if [ ! -e 'obj/refs.zip' ]; then
    wget -O obj/refs.zip https://dotnet.myget.org/F/aspnetcore-tools/api/v2/package/NETFrameworkReferenceAssemblies/$netfxversion
    unzip -q -d obj/refs/ obj/refs.zip
fi

dotnet_home="$(pwd)/.dotnet"
artifacts="$(pwd)/artifacts"

rm -r "$artifacts" 2>/dev/null && :
ensure_dotnet $dotnet_home 1.0.1
echo "dotnet = $(dotnet --version)"
__exec dotnet restore
__exec dotnet msbuild /nologo src/Yarn.MSBuild/ /t:GetYarn
__exec dotnet pack -c $config -o "$artifacts"
__exec dotnet test -c $config test/Yarn.MSBuild.Tests/Yarn.MSBuild.Tests.csproj
