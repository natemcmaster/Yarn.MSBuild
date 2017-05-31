using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using NuGet.Versioning;
using Xunit;

namespace Yarn.MSBuild.Tests.Utilities
{
    [CollectionDefinition(TestProjectManager.Collection)]
    public class TestProjectCollection : ICollectionFixture<TestProjectManager>
    { }

    public class TestProjectManager : IDisposable
    {
        public const string Collection = "testapps";
        private readonly string _baseDir;
        private readonly string _workDir;
        private Stack<IDisposable> _disposables
            = new Stack<IDisposable>();

        private Dictionary<string, string> _envVariables
            = new Dictionary<string, string>();

#if NETCOREAPP1_1
        private static readonly string s_baseDir = AppContext.BaseDirectory;
#elif NET461
        private static readonly string s_baseDir = AppDomain.CurrentDomain.BaseDirectory;
#else
#error Target frameworks need to be updated
#endif

        public TestProjectManager()
        {
            var tfm = Path.GetFileName(s_baseDir);
            var project = new DirectoryInfo(s_baseDir) // tfm
                .Parent // Debug
                .Parent // bin
                .Parent; // proj dir

            _baseDir = Path.Combine(project.Parent.FullName, "testapps");
            _workDir = Path.Combine(project.FullName, "obj", "testapps", tfm);
            Directory.CreateDirectory(_workDir);
            File.Copy(
                Path.Combine(_baseDir, "NuGet.config"),
                Path.Combine(_workDir, "NuGet.config"),
                overwrite: true);

            var artifacts = Path.Combine(project.Parent.Parent.FullName, "artifacts");
            var home = Environment.GetEnvironmentVariable("USERPROFILE")
                ?? Environment.GetEnvironmentVariable("HOME")
                ?? Environment.GetEnvironmentVariable("HOMEDRIVE");

            _envVariables["ARTIFACTS_PATH"] = artifacts;
            _envVariables["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = bool.TrueString;

            var packageId = "Yarn.MSBuild";
            DeletePackage(home, packageId);
            DeletePackage(home, "Yarn.MSBuild.Min");

            var yarnVersion = Directory.EnumerateFiles(artifacts, "*.nupkg")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Where(f => f.StartsWith(packageId) && !f.Contains("Min"))
                .Select(f => NuGetVersion.Parse(f.Substring(packageId.Length + 1)))
                .Single();
            _envVariables["TestPackageVersion"] = yarnVersion.ToNormalizedString();
        }

        private static void DeletePackage(string home, string packageId)
        {
            var packagesDir = Environment.GetEnvironmentVariable("NUGET_PACKAGES") ?? Path.Combine(home, ".nuget", "packages");
            var yarnPkgDir = Path.Combine(packagesDir, packageId.ToLowerInvariant());
            if (Directory.Exists(yarnPkgDir))
            {
                Console.WriteLine($"Deleting {yarnPkgDir}");
                Directory.Delete(yarnPkgDir, recursive: true);
            }
        }

        public void Dispose()
        {
            while (_disposables.Count > 0)
            {
                _disposables.Pop().Dispose();
            }
        }

        public TempProj Create(string testAppName)
        {
            var source = Path.Combine(_baseDir, testAppName);
            var tempDir = Path.Combine(_workDir, Guid.NewGuid().ToString());
            CopyRecursive(source, tempDir);
            var tmp = new TempProj(_envVariables, tempDir);
            _disposables.Push(tmp);
            return tmp;
        }

        private static void CopyRecursive(string src, string dest)
        {
            if (src[src.Length - 1] != '/' || src[src.Length - 1] != '\\')
            {
                src += Path.DirectorySeparatorChar;
            }

            Directory.CreateDirectory(dest);
            foreach (var fileSrc in Directory.EnumerateFiles(src, "*", SearchOption.AllDirectories))
            {
                var relPath = fileSrc.Substring(src.Length);
                var fileDest = Path.Combine(dest, relPath);
                Directory.CreateDirectory(Path.GetDirectoryName(fileDest));
                File.Copy(fileSrc, fileDest);
            }
        }
    }
}
