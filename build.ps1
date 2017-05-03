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

echo "dotnet = $(dotnet --version)"

$proj_dir = "$PSScriptRoot/src/Yarn.MSBuild"
$dist_dir = "$proj_dir/dist"
if (Test-Path $dist_dir) {
    rm -r $dist_dir
}

if (!(Test-Path tools/7z.exe)) {
    mkdir tools/ -ErrorAction Ignore | Out-Null
    iwr http://www.7-zip.org/a/7z1604-x64.exe -OutFile tools/7z.exe
}

$yarn_archive = "dist/yarn-v$yarn_version.tar.gz"
if (!(Test-Path $yarn_archive)) {
    write-host -ForegroundColor Cyan "Downloading $yarn_archive"
    mkdir dist/ -ErrorAction Ignore | Out-Null
    iwr https://github.com/yarnpkg/yarn/releases/download/v$yarn_version/yarn-v$yarn_version.tar.gz -outfile $yarn_archive
}
rm -r "$proj_dir/dist" -ErrorAction Ignore | Out-Null
__exec tools/7z.exe x -y -tgzip "-o${env:TEMP}" $yarn_archive
__exec tools/7z.exe x -y -ttar "-o$proj_dir" "${env:TEMP}/yarn-v$yarn_version.tar" 

__exec dotnet restore /p:VersionPrefix=$yarn_version
__exec dotnet pack --configuration $config --output $artifacts /p:VersionPrefix=$yarn_version
__exec dotnet test --configuration $config test/Yarn.MSBuild.Tests/Yarn.MSBuild.Tests.csproj
