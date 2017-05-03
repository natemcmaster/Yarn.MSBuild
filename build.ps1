#!/usr/bin/env powershell

function __exec($cmd) {
    write-host -fg Cyan "> $cmd $args"
    & $cmd @args
    $exit_code = $LASTEXITCODE
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

iwr https://github.com/yarnpkg/yarn/releases/download/v$yarn_version/yarn-v$yarn_version.tar.gz -outfile $env:TEMP/yarn.tar.gz
__exec 7z x -y -so $env:TEMP/yarn.tar.gz | 7z x -y -si -ttar -o $proj_dir
rm $env:TEMP/yarn.tar.gz

__exec dotnet restore /p:VersionPrefix=$yarn_version
__exec dotnet pack -c $config -o $artifacts /p:VersionPrefix=$yarn_version
__exec dotnet test -c $config test/Yarn.MSBuild.Tests/Yarn.MSBuild.Tests.csproj
