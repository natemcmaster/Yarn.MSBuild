using System.Runtime.InteropServices;
using FluentAssertions;
using Xunit;
using Yarn.MSBuild.Tests.Utilities;

namespace Yarn.MSBuild.Tests
{
    [Collection(TestProjectManager.Collection)]
    public class YarnTaskTests
    {
        private readonly TestProjectManager _projManager;

        public YarnTaskTests(TestProjectManager projManager)
        {
            _projManager = projManager;
        }

        [Fact]
        public void InstallsOnBuild()
        {
            var proj = _projManager.Create("WebSdkProj");
            proj.Restore().Should().Pass();
            proj.Root.Should().NotHaveFile("yarn.lock");
            proj.Build().Should().Pass();
            proj.Root.Should().HaveFile("yarn.lock");
            proj.Done();
        }

        [Fact]
        public void ShouldRunOnMultiTfmProjects()
        {
            var proj = _projManager.Create("MultiTfmWebApp");
            proj.Restore().Should().Pass();

            proj.Root.Should().NotHaveFile("yarn.lock");
            proj.Build().Should().Pass();
            proj.Root.Should().HaveFile("yarn.lock");

            proj.Root.GetFile("yarn.lock").Delete();
            proj.Build("/p:TargetFramework=netcoreapp1.1").Should().Pass();
            proj.Root.Should().HaveFile("yarn.lock");

#if NETCOREAPP1_1
            var secondTfm = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "net46"
                : "netcoreapp1.0";
#elif NET461
            var secondTfm = "net46";
#else
#error Target frameworks need updating
#endif

            proj.Root.GetFile("yarn.lock").Delete();
            proj.Build($"/p:TargetFramework={secondTfm}").Should().Pass();
            proj.Root.Should().HaveFile("yarn.lock");

            proj.Done();
        }

        [Fact]
        public void RunsOtherYarnCommands()
        {
            var proj = _projManager.Create("YarnCommands");
            proj.Restore().Should().Pass();
            proj.Root.Should().NotHaveFile("testran.txt");
            proj.Msbuild("/t:RunYarnTest").Should().Pass();
            proj.Root.Should().HaveFile("testran.txt");
            proj.Done();
        }
    }
}
