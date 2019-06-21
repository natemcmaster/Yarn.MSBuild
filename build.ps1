#!/usr/bin/env pwsh
[CmdletBinding(PositionalBinding = $false)]
param(
    [switch]
    $ci,
    [switch]
    $sign
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 1

function exec($cmd) {
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

[string[]] $MSBuildArgs = @('-nodeReuse:false')

if ($ci) {
    $MSBuildArgs += '-p:CI=true'
}

$CodeSign = $sign -or ($ci -and ($env:BUILD_REASON -ne 'PullRequest') -and ($IsWindows -or -not $IsCoreCLR))

if ($CodeSign) {
    $toolsDir = "$PSScriptRoot/.build/tools"
    $AzureSignToolPath = "$toolsDir/azuresigntool"
    if ($IsWindows) {
        $AzureSignToolPath += ".exe"
    }

    if (-not (Test-Path $AzureSignToolPath)) {
        exec dotnet tool install --tool-path $toolsDir `
        AzureSignTool `
        --version 2.0.17
    }

    $nstDir = "$toolsDir/nugetsigntool/1.1.4"
    $NuGetKeyVaultSignToolPath = "$nstDir/tools/net471/NuGetKeyVaultSignTool.exe"
    if (-not (Test-Path $NuGetKeyVaultSignToolPath)) {
        New-Item $nstDir -ItemType Directory -ErrorAction Ignore | Out-Null
        Invoke-WebRequest https://github.com/onovotny/NuGetKeyVaultSignTool/releases/download/v1.1.4/NuGetKeyVaultSignTool.1.1.4.nupkg `
            -OutFile "$nstDir/NuGetKeyVaultSignTool.zip"
        Expand-Archive "$nstDir/NuGetKeyVaultSignTool.zip" -DestinationPath $nstDir
    }

    $MSBuildArgs += '-p:CodeSign=true'
    $MSBuildArgs += "-p:AzureSignToolPath=$AzureSignToolPath"
    $MSBuildArgs += "-p:NuGetKeyVaultSignToolPath=$NuGetKeyVaultSignToolPath"
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
    # github requires TLS 1.2
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
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
    exec ./7za.exe x -y -tgzip "-o${env:TEMP}" $yarn_archive
    exec ./7za.exe x -y -ttar "-o$dist_dir" "${env:TEMP}/yarn-v$yarn_version.tar"
    gci "$dist_dir/yarn-v$yarn_version/*" | % { mv $_ $dist_dir }
}
finally {
    rm 7za.exe
}

$commit = ''
if (Get-Command git) {
    $commit = git rev-parse HEAD
}

exec dotnet build `
    --configuration $config `
    "-p:RepositoryCommit=$commit" `
    @MSBuildArgs

exec dotnet test --no-build --no-restore `
    --configuration $config `
    test/Yarn.MSBuild.Tests/Yarn.MSBuild.Tests.csproj `
    @MSBuildArgs
