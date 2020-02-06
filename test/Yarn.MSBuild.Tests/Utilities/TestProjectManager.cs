// Copyright (c) Nate McMaster.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Versioning;
using Xunit;
using Xunit.Abstractions;

namespace Yarn.MSBuild.Tests.Utilities
{
    [CollectionDefinition(TestProjectManager.Collection, DisableParallelization = true)]
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

        public TestProjectManager()
        {
            var tfm = Path.GetFileName(AppContext.BaseDirectory);
            var project = new DirectoryInfo(AppContext.BaseDirectory) // tfm
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
            _envVariables["MSBUILDDISABLENODEREUSE"] = "1";

            var packageId = "Yarn.MSBuild";
            var packagesDir = Environment.GetEnvironmentVariable("NUGET_PACKAGES") ?? Path.Combine(home, ".nuget", "packages");
            var yarnPkgDir = Path.Combine(packagesDir, packageId.ToLowerInvariant());
            if (Directory.Exists(yarnPkgDir))
            {
                Console.WriteLine($"Deleting {yarnPkgDir}");
                Directory.Delete(yarnPkgDir, recursive: true);
            }

            var yarnVersion = Directory.EnumerateFiles(artifacts, "*.nupkg")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Where(f => f.StartsWith(packageId))
                .Select(f => NuGetVersion.Parse(f.Substring(packageId.Length + 1)))
                .Single();
            _envVariables["TestPackageVersion"] = yarnVersion.ToNormalizedString();

            File.WriteAllText(
                Path.Combine(_workDir, "global.json"),
                @"{""msbuild-sdks"": {""Yarn.MSBuild"": """ + yarnVersion + @"""}}");
        }

        public void Dispose()
        {
            while (_disposables.Count > 0)
            {
                _disposables.Pop().Dispose();
            }
        }

        public TempProj Create(string testAppName, ITestOutputHelper output)
        {
            var source = Path.Combine(_baseDir, testAppName);
            var tempDir = Path.Combine(_workDir, Guid.NewGuid().ToString());
            CopyRecursive(source, tempDir);
            var tmp = new TempProj(_envVariables, tempDir, output);
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
