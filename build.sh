#!/usr/bin/env bash

set -euo pipefail

RED='\033[31m'
CYAN='\033[36m'
YELLOW='\033[33m'
RESET='\033[0m'

__exec() {
    local cmd=$1
    shift
    echo -e "${CYAN}>>> $cmd $@${RESET}"
    set +e
    $cmd $@
    local exit_code=$?
    set -e
    if [ $exit_code -ne 0 ]; then
        echo -e "${RED}Failed with exit code $exit_code${RESET}"
        exit 1
    fi
}

config='Release'
artifacts="$(pwd)/artifacts"

rm -r "$artifacts" 2>/dev/null && :

yarn_version=$(<yarn.version)
export YarnVersion=$yarn_version
proj_dir="$(pwd)/src/Yarn.MSBuild"
dist_dir="$proj_dir/dist"
yarn_archive="dist/yarn-v$yarn_version.tar.gz"
if [ ! -f "$yarn_archive" ]; then
    mkdir -p dist/
    __exec wget -O "$yarn_archive" https://github.com/yarnpkg/yarn/releases/download/v$yarn_version/yarn-v$yarn_version.tar.gz
fi
rm -r "$dist_dir" 2>/dev/null && :
mkdir -p "$dist_dir"
__exec tar -zx -C "$dist_dir" -f "$yarn_archive"
mv $dist_dir/yarn-v$yarn_version/* "$dist_dir/"

__exec dotnet restore
__exec dotnet build --no-restore --configuration $config
__exec dotnet test --no-restore --no-build --configuration $config test/Yarn.MSBuild.Tests/Yarn.MSBuild.Tests.csproj
