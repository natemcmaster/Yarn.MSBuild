#!/usr/bin/env pwsh
[CmdletBinding(PositionalBinding = $false)]
param(
    [ValidateSet('Debug', 'Release')]
    $Configuration = $null,
    [switch]
    $ci,
    [switch]
    $sign,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$MSBuildArgs
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 1

function exec([string]$_cmd) {
    write-host -ForegroundColor DarkGray ">>> $_cmd $args"
    $ErrorActionPreference = 'Continue'
    & $_cmd @args
    $ErrorActionPreference = 'Stop'
    if ($LASTEXITCODE -ne 0) {
        write-error "Failed with exit code $LASTEXITCODE"
        exit 1
    }
}

#
# Main
#

$MSBuildArgs += '-nodeReuse:false'

$isPr = $env:BUILD_REASON -eq 'PullRequest'
if (-not (Test-Path variable:\IsCoreCLR)) {
    $IsWindows = $true
}

if ($env:CI -eq 'true') {
    $ci = $true
    & dotnet --info
}

if (!$Configuration) {
    $Configuration = if ($ci) { 'Release' } else { 'Debug' }
}

if ($ci) {
    $MSBuildArgs += '-p:CI=true'
}

$CodeSign = $sign -or ($ci -and -not $isPr -and $IsWindows)

if ($CodeSign) {
    $MSBuildArgs += '-p:CodeSign=true'
}

$artifacts = "$PSScriptRoot/artifacts/"

Remove-Item -Recurse $artifacts -ErrorAction Ignore


$pkg_json = Get-Content -Raw "$PSScriptRoot/src/Yarn.MSBuild/package.json" | ConvertFrom-Json
$yarn_version = $pkg_json.dependencies.yarn
$MSBuildArgs += "-p:YarnVersion=$yarn_version"

exec dotnet tool restore

$proj_dir = "$PSScriptRoot/src/Yarn.MSBuild"
$dist_dir = "$proj_dir/dist"
$yarn_archive = "$PSScriptRoot/dist/yarn-v$yarn_version.tar.gz"
if (!(Test-Path $yarn_archive)) {
    write-host -ForegroundColor Cyan "Downloading yarn-v$yarn_version.tar.gz"
    New-Item -type Directory dist/ -ErrorAction Ignore | Out-Null
    # github requires TLS 1.2
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    iwr https://github.com/yarnpkg/yarn/releases/download/v$yarn_version/yarn-v$yarn_version.tar.gz -outfile $yarn_archive
}

Remove-Item -recurse -force $dist_dir -ErrorAction Ignore | Out-Null
New-Item -type Directory $dist_dir | Out-Null
exec tar -zx -C $dist_dir -f $yarn_archive
Get-ChildItem "$dist_dir/yarn-v$yarn_version/*" | % { Move-Item $_ $dist_dir }

if ($IsWindows -and -not (Get-Command msbuild -ErrorAction Ignore)) {
    $vsWherePath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path $vsWherePath)) {
        Write-Warning "Could not find Visual Studio and MSBuild.exe"
    }
    else {
        $vs = & $vsWherePath -latest -products * -format json | ConvertFrom-Json | select -first 1
        Write-Host "Found VS in $($vs.installationPath)"
        if ($ci) {
            Write-Host "##vso[task.prependpath]$($vs.installationPath)\MSBuild\Current\Bin"
        }

        $env:PATH = "$env:PATH;$($vs.installationPath)\MSBuild\Current\Bin"
    }
}

exec dotnet build --configuration $Configuration @MSBuildArgs

[string[]] $testArgs=@()
if (-not $IsWindows) {
    $testArgs += '-p:TestFullFramework=false'
}
if ($env:TF_BUILD) {
    $testArgs += '--logger', 'trx'
}

exec dotnet test --no-build --no-restore --configuration $Configuration `
    @testArgs `
    @MSBuildArgs
