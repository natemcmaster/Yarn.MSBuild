// Copyright (c) Nate McMaster.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
    /// <summary>
    /// A task for invoking the bundled version of yarn
    /// </summary>
    public class Yarn : MSBuildTask
    {
        private const string YarnExeName = "yarn";

        /// <summary>
        /// Command line arguments to be passed to yarn
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// The current working directory for the yarn process
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Override the path to the yarn executable. When empty, the task binds the version bundled in with this task.
        /// </summary>
        public string ExecutablePath { get; set; }

        /// <summary>
        /// The path to the nodejs executable. If not specified, this task will expect node to be in PATH.
        /// </summary>
        public string NodeJsExecutablePath { get; set; }

        /// <summary>
        /// Ignore the exit code of yarn.
        /// </summary>
        public bool IgnoreExitCode { get; set; }

        /// <summary>
        /// The exit code of the process.
        /// </summary>
        [Output]
        public int ExitCode { get; set; }

        public override bool Execute()
        {
            var dir = GetCwd();
            if (dir == null)
            {
                return false;
            }

            var settings = GetYarnExe();

            var path = Environment.GetEnvironmentVariable("PATH");

            if (!string.IsNullOrEmpty(NodeJsExecutablePath))
            {
                if (!Path.IsPathRooted(NodeJsExecutablePath))
                {
                    Log.LogWarning(nameof(NodeJsExecutablePath) + " is not an absolute path, so its value is being ignored. Pass in an absolute path to nodejs instead.");
                }
                else
                {
                    var nodeDir = Path.GetDirectoryName(NodeJsExecutablePath);
                    // prepend the node directory so it is found first in the system lookup for nodejs
                    path = nodeDir + Path.PathSeparator + path;
                    Log.LogMessage(MessageImportance.Low, "Adding {0} to the system PATH", nodeDir);
                }
            }

            var process = new Process
            {
                StartInfo =
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    FileName = settings.Item1,
                    Arguments = settings.Item2,
                    WorkingDirectory = dir,
                    Environment =
                    {
                        ["PATH"] = path,
                    }
                }
            };

            Log.LogMessage(MessageImportance.Low, process.StartInfo.FileName + " " + process.StartInfo.Arguments);

            var displayArgs = !string.IsNullOrEmpty(Command)
                ? " " + Command
                : null;
            Log.LogCommandLine("yarn" + displayArgs);

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
            var success = process.ExitCode == 0;
            ExitCode = process.ExitCode;
            return success || IgnoreExitCode;
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
#if NET46
            // This means the task is running on MSBuild.exe, which is usually only Windows.
            return Type.GetType("Mono.Runtime", throwOnError: false) == null;
#elif NETSTANDARD1_5
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#else
#error Target framework needs to be updated
#endif
        }
    }
}
