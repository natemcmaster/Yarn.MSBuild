#!/usr/bin/env bash

set -e
source ./scripts/common.sh

if [ "$NUGET_API_KEY" = "" ]; then
    echo -e "${YELLOW}Skipping nuget push because NUGET_API_KEY was not set${RESET}"
else
    dotnet nuget push \
        --source https://api.nuget.org/v3/index.json \
        --api-key "$NUGET_API_KEY" \
        artifacts/*.nupkg
fi
