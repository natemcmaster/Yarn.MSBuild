#!/usr/bin/env powershell

$ErrorActionPreference = 'Stop'

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
if (Test-Path $dist_dir) {
    rm -r $dist_dir
}

$yarn_archive = "$PSScriptRoot/dist/yarn-v$yarn_version.tar.gz"
if (!(Test-Path $yarn_archive)) {
    write-host -ForegroundColor Cyan "Downloading yarn-v$yarn_version.tar.gz"
    mkdir dist/ -ErrorAction Ignore | Out-Null
    iwr https://github.com/yarnpkg/yarn/releases/download/v$yarn_version/yarn-v$yarn_version.tar.gz -outfile $yarn_archive
}

if (!(Test-Path tools/7z.exe)) {
    write-host -ForegroundColor Cyan "Downloading 7z.exe"
    mkdir tools/ -ErrorAction Ignore | Out-Null
    iwr http://www.7-zip.org/a/7z1604-x64.exe -OutFile tools/7z.exe
}

cp tools/7z.exe ./
try {
    __exec 7z x -y -tgzip "-o${env:TEMP}" $yarn_archive
    __exec 7z x -y -ttar "-o$proj_dir" "${env:TEMP}/yarn-v$yarn_version.tar" 
} finally {
    rm 7z.exe
}

__exec dotnet restore
__exec dotnet pack --configuration $config --output $artifacts
__exec dotnet test --configuration $config test/Yarn.MSBuild.Tests/Yarn.MSBuild.Tests.csproj
