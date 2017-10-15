Yarn.MSBuild
============

[![Travis][travis-badge]](https://travis-ci.org/natemcmaster/Yarn.MSBuild)
[![AppVeyor][appveyor-badge]](https://ci.appveyor.com/project/natemcmaster/yarn-msbuild)
[![NuGet][nuget-badge]](https://nuget.org/packages/Yarn.MSBuild)
[![MyGet][myget-badge]](https://www.myget.org/feed/natemcmaster/package/nuget/Yarn.MSBuild)

[travis-badge]: https://img.shields.io/travis/natemcmaster/Yarn.MSBuild.svg?style=flat-square&label=travis
[appveyor-badge]: https://img.shields.io/appveyor/ci/natemcmaster/yarn-msbuild.svg?style=flat-square&label=appveyor
[nuget-badge]: https://img.shields.io/nuget/v/Yarn.MSBuild.svg?style=flat-square&label=nuget
[myget-badge]: https://img.shields.io/www.myget/natemcmaster/vpre/Yarn.MSBuild.svg?style=flat-square&label=myget

An MSBuild task for running the Yarn package manager.

See [Yarn's Official Website](https://yarnpkg.com/en/) for more information about using Yarn.

# Installation

**Package Manager Console in Visual Studio**
```
PM> Install-Package Yarn.MSBuild
```

**.NET Core Command Line**
```
dotnet add package Yarn.MSBuild
```

**In csproj**
```xml
<ItemGroup>
  <PackageReference Include="Yarn.MSBuild" Version="1.1.0" />
</ItemGroup>
```

# Usage

## Default usage

This package is designed for use with ASP.NET Core projects.

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>netcoreapp2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.All" Version="2.0.0" />
    <PackageReference Include="Yarn.MSBuild" Version="1.1.0" />
  </ItemGroup>
</Project>
```

Project layout:
```
+ WebApplication.csproj
+ package.json
+ Startup.cs
- wwwroot
   + app.js
   + site.css
```

Running `dotnet build` or `msbuild.exe /t:Build` will automatically invoke `yarn install`.

### Additional options

```xml
<PropertyGroup>
  <!-- Prevent yarn from running on 'Build'. Default to 'false'-->
  <SuppressAutoYarn>true</SuppressAutoYarn>

  <!-- Change the yarn that runs on 'Build'. Defaults to 'install'. -->
  <YarnBuildCommand>run build</YarnBuildCommand>

  <!-- Change the directory in which yarn is invoked on build. Defaults to '$(MSBuildProjectDirectory)'. -->
  <YarnDir>$(MSBuildProjectDirectory)/wwwroot/</YarnDir>

  <!-- Specify the default path to NodeJS. -->
  <NodeJSExecutablePath>/opt/nodejs/bin/node</NodeJSExecutablePath>
</PropertyGroup>
```

## Using the task

The `Yarn` task supports the following parameters
```
[Optional]
string Command                The arguments to pass to yarn.

[Optional]
string ExecutablePath         Where to find yarn (*nix) or yarn.cmd (Windows)

[Optional]
string NodeJsExecutablePath   Where to find node(js) (*nix) or node.cmd (Windows). 
                              If not provided, node is expected to be in the PATH environment variable.

[Optional]
string WorkingDirectory       The directory in which to execute the yarn command

[Optional]
bool IgnoreExitCode           Don't create and error if the exit code is non-zero
```

Task outputs:
```
[Output]
int ExitCode                  Returns the exit code of the yarn process
```

```xml
<Project>
  <Target Name="RunYarnCommands">
    <!-- defaults to "install" in the current directory using the bundled version of yarn. -->
    <Yarn />

    <Yarn Command="upgrade" />

    <Yarn Command="run test" WorkingDirectory="wwwroot/" />
    <Yarn Command="run cmd" ExecutablePath="/usr/local/bin/yarn" />
  </Target>
</Project>
```

# About

This is not an official Yarn project. See [LICENSE.txt](LICENSE.txt) and the [Third Party Notice](src/Yarn.MSBuild/third_party_notice.txt) for more details.
