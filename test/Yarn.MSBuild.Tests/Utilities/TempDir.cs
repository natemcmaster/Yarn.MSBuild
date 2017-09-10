// Copyright (c) Nate McMaster.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Yarn.MSBuild.Tests.Utilities
{
    public class TempDir : IDisposable
    {
        public TempDir(string root)
        {
            Root = new DirectoryInfo(root);
        }

        public DirectoryInfo Root { get; }

        public virtual void Dispose()
        {
            Root.Delete(true);
        }
    }
}
