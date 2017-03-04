using System.Diagnostics;
using System.IO;
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

            var (exe, args) = GetYarnExe();

            var process = new Process
            {
                StartInfo = 
                {
                    FileName = exe,
                    Arguments = args,
                    WorkingDirectory = dir,
                }
            };

            var displayArgs = !string.IsNullOrEmpty(Command)
                ? " " + Command
                : null;
            Log.LogMessage(MessageImportance.High, "Executing 'yarn{0}'", displayArgs);
            
            process.Start();
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

        private (string exe, string args) GetYarnExe()
        {
            string exe;
            if (string.IsNullOrEmpty(ExecutablePath))
            {
                exe = YarnExeName;
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

            return (exe, args);
        }

        private static bool IsWindows()
        {
#if NET46
            // This means the task is running on MSBuild.exe, with is only supported on Windows
            return true;
#else
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#endif
        }
    }
}
