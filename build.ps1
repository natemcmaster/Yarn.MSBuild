#!/usr/bin/env pwsh

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2

function __exec($cmd) {
    write-host -ForegroundColor Cyan "> $cmd $args"
    $ErrorActionPreference = 'Continue'
    & $cmd @args
    $exit_code = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    if ($exit_code -ne 0) {
        write-error "Failed with exit code $exit_code"
        exit 1
    }
}

$config = 'Release'
$artifacts = "$PSScriptRoot/artifacts"
if (Test-Path $artifacts) {
    rm -r $artifacts
}

$yarn_version = Get-Content "$PSScriptRoot/yarn.version"
$env:YarnVersion = $yarn_version

echo "dotnet = $(dotnet --version)"

$proj_dir = "$PSScriptRoot/src/Yarn.MSBuild"
$dist_dir = "$proj_dir/dist"
$yarn_archive = "$PSScriptRoot/dist/yarn-v$yarn_version.tar.gz"
if (!(Test-Path $yarn_archive)) {
    write-host -ForegroundColor Cyan "Downloading yarn-v$yarn_version.tar.gz"
    mkdir dist/ -ErrorAction Ignore | Out-Null
    iwr https://github.com/yarnpkg/yarn/releases/download/v$yarn_version/yarn-v$yarn_version.tar.gz -outfile $yarn_archive
}

if (!(Test-Path tools/7za.exe)) {
    write-host -ForegroundColor Cyan "Downloading 7za.exe"
    mkdir tools/ -ErrorAction Ignore | Out-Null
    iwr http://www.7-zip.org/a/7za920.zip -OutFile tools/7za.zip
    Expand-Archive tools/7za.zip -DestinationPath ./tools
}

cp tools/7za.exe ./
try {
    rm -recurse -force $dist_dir -ErrorAction Ignore
    mkdir $dist_dir
    __exec ./7za.exe x -y -tgzip "-o${env:TEMP}" $yarn_archive
    __exec ./7za.exe x -y -ttar "-o$dist_dir" "${env:TEMP}/yarn-v$yarn_version.tar"
    gci "$dist_dir/yarn-v$yarn_version/*" | % { mv $_ $dist_dir }
} finally {
    rm 7za.exe
}

__exec dotnet restore
__exec dotnet build --no-restore --configuration $config
__exec dotnet test --no-build --no-restore --configuration $config test/Yarn.MSBuild.Tests/Yarn.MSBuild.Tests.csproj
