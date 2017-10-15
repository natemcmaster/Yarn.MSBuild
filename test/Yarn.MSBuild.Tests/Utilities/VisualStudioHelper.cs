using System;
using System.Diagnostics;
using System.IO;

namespace Yarn.MSBuild.Tests.Utilities
{
    public class VisualStudioHelper
    {
        static VisualStudioHelper()
        {
            VsWhere = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Visual Studio", "Installer", "vswhere.exe");

            if (File.Exists(VsWhere))
            {
                try
                {
                    var vswhereProcess = Process.Start(new ProcessStartInfo
                    {
                        FileName = VsWhere,
                        Arguments = "-latest -property installationPath",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                    });

                    vswhereProcess.WaitForExit();

                    var output = vswhereProcess.StandardOutput.ReadToEnd()?.Trim() ?? string.Empty;

                    InstallationPath = output;
                }
                catch
                {
                }
            }
        }

        public static string VsWhere { get; }
        public static string InstallationPath { get; }
    }
}
