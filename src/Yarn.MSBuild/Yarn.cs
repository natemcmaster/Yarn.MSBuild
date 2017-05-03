using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Build.Framework;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Yarn.MSBuild
{
    public class Yarn : MSBuildTask
    {
        private const string YarnExeName = "yarn";

        public string Command { get; set; }

        public string WorkingDirectory { get; set; }

        public string ExecutablePath { get; set; }

        public override bool Execute()
        {
            var dir = GetCwd();
            if (dir == null)
            {
                return false;
            }

            var settings = GetYarnExe();

            var process = new Process
            {
                StartInfo = 
                {
                    FileName = settings.Item1,
                    Arguments = settings.Item2,
                    WorkingDirectory = dir,
                }
            };

            var displayArgs = !string.IsNullOrEmpty(Command)
                ? " " + Command
                : null;
            Log.LogMessage(MessageImportance.High, "Executing 'yarn{0}'", displayArgs);

            try
            {
                process.Start();
            }
            catch (Win32Exception)
            {
                Log.LogError($"Failed to start yarn from '{settings.Item1}'. You can override this by setting the {nameof(ExecutablePath)} property on the Yarn task.");
                return false;
            }

            process.WaitForExit();
            return process.ExitCode == 0;
        }

        private string GetCwd()
        {
            if (string.IsNullOrEmpty(WorkingDirectory))
            {
                return Directory.GetCurrentDirectory();
            }

            if (!Directory.Exists(WorkingDirectory))
            {
                Log.LogError("WorkingDirectory does not exist: '{0}", WorkingDirectory);
                return null;
            }

            return WorkingDirectory;
        }

        private Tuple<string, string> GetYarnExe()
        {
            string exe;
            if (string.IsNullOrEmpty(ExecutablePath))
            {
                exe = FindBundledYarn();
                if (!File.Exists(exe))
                {
                    Log.LogMessage("Failed to find the version of yarn bundled in this package. [{0}]", exe);
                    exe = YarnExeName;
                }
            }
            else if (!File.Exists(ExecutablePath))
            {
                Log.LogWarning(
                    "The file path set on ExecutablePath does not exist: '{0}'. " +
                    "Falling back to using the system PATH.", ExecutablePath);
                exe = YarnExeName;
            }
            else
            {
                exe = ExecutablePath;
            }

            string args;
            if (IsWindows())
            {
                args = $"/C \"{exe}\" {Command}";
                exe = "cmd";
            }
            else
            {
                args = Command;
            }

            return Tuple.Create(exe, args);
        }

        private string FindBundledYarn()
        {
            var assembly = typeof(Yarn).GetTypeInfo().Assembly.Location;
            var nugetRoot = new FileInfo(assembly) // Yarn.MSBuild.dll
                .Directory // tfm
                .Parent // tools
                .Parent.FullName; // nuget package
            return Path.Combine(nugetRoot, "dist/bin/yarn");
        }

        private static bool IsWindows()
        {
#if NET461
            // This means the task is running on MSBuild.exe, with is only supported on Windows
            return true;
#elif NETSTANDARD1_6
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#else
#error Target framework needs to be updated
#endif
        }
    }
}
