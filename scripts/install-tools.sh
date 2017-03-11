#!/usr/bin/env bash

set -o pipefail

_download() {
    curl -sSL https://raw.githubusercontent.com/dotnet/cli/rel/1.0.1/scripts/obtain/dotnet-install.sh \
        | bash -s -- -i $dir -v $version
}

install_dotnet() {
    dir=$1
    shift
    version=$1
    shift
    if ! which dotnet >/dev/null
    then
        _download $dir $version
    else
        current_version="$(dotnet --version || echo '')"
        if [ "$current_version" -ne "$version" ]
        then
            _download $dir $version
        fi
    fi
}
