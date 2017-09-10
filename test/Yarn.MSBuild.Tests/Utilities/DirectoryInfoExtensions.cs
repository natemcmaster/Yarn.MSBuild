// Copyright (c) Nate McMaster.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Yarn.MSBuild.Tests.Utilities;

namespace FluentAssertions
{
    public static class DirectoryInfoExtensions
    {
        public static DirectoryInfoAssertions Should(this DirectoryInfo dir)
        {
            return new DirectoryInfoAssertions(dir);
        }

        public static DirectoryInfo Sub(this DirectoryInfo dir, string name)
        {
            return new DirectoryInfo(Path.Combine(dir.FullName, name));
        }

        public static bool Contains(this DirectoryInfo subject, FileSystemInfo target)
        {
            return target.FullName.StartsWith(subject.FullName);
        }

        public static DirectoryInfo GetDirectory(this DirectoryInfo subject, params string [] directoryNames)
        {
            return new DirectoryInfo(Path.Combine(subject.FullName, Path.Combine(directoryNames)));
        }

        public static FileInfo GetFile(this DirectoryInfo subject, string fileName)
        {
            return new FileInfo(Path.Combine(subject.FullName, fileName));
        }
    }
}
