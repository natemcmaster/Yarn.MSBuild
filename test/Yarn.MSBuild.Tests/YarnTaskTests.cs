// Copyright (c) Nate McMaster.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using Yarn.MSBuild.Tests.Utilities;

namespace Yarn.MSBuild.Tests
{
    [Collection(TestProjectManager.Collection)]
    public class YarnTaskTests
    {
        private readonly TestProjectManager _projManager;
        private readonly ITestOutputHelper _output;

        public YarnTaskTests(TestProjectManager projManager, ITestOutputHelper output)
        {
            _projManager = projManager;
            _output = output;
        }

        [Fact]
        public void RunsYarnInstalledAsAnSdkPackage()
        {
            var proj = _projManager.Create("SdkProj", _output);
            proj.Root.Should().NotHaveFile("yarn.lock");
            proj.Build().Should().Pass();
            proj.Root.Should().HaveFile("yarn.lock");
            proj.Done();
        }

        [Fact]
        public void RunsYarnBuildCommand()
        {
            var proj = _projManager.Create("WebSdkProj", _output);
            proj.Root.Should().NotHaveFile("yarn.lock");
            proj.Build().Should().Pass();
            proj.Root.Should().HaveFile("yarn.lock");
            proj.Done();
        }

        [Fact]
        public void LoggingDetectsWarning()
        {
            var proj = _projManager.Create("ProjWithWarnings", _output);
            proj.Build()
                .Should().Pass()
                .And
                .ContainStdOut("warning : \" > ts-jest@22.4.6\" has incorrect peer dependency");
            proj.Done();
        }

        [Fact]
        public void ShouldRunOnMultiTfmProjects()
        {
            var proj = _projManager.Create("MultiTfmWebApp", _output);

            proj.Root.Should().NotHaveFile("yarn.lock");
            proj.Build().Should().Pass();
            proj.Root.Should().HaveFile("yarn.lock");

            proj.Root.GetFile("yarn.lock").Delete();
            proj.Build("-p:TargetFramework=netcoreapp2.1").Should().Pass();
            proj.Root.Should().HaveFile("yarn.lock");

            proj.Root.GetFile("yarn.lock").Delete();
            proj.Build($"-p:TargetFramework=netcoreapp3.1").Should().Pass();
            proj.Root.Should().HaveFile("yarn.lock");

            proj.Done();
        }

        [Fact]
        public void RunsOtherYarnCommands()
        {
            var proj = _projManager.Create("YarnCommands", _output);
            proj.Root.Should().NotHaveFile("testran.txt");
            proj.Msbuild("-restore", "-t:RunYarnTest").Should().Pass();
            proj.Root.Should().HaveFile("testran.txt");
            proj.Msbuild("-t:RunYarnTest1").Should().Pass();
            proj.Root.Should().HaveFile("testran2.txt");
            proj.Done();
        }
    }
}
