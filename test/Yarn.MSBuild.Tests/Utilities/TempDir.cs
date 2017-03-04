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
